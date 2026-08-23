# VPS desde cero — instalación y seguridad

Guía completa para dejar un servidor en un estado **coherente y predecible**:
un solo firewall, un solo lugar donde se decide qué está expuesto, y una regla
clara para cada tipo de servicio.

Está escrita contra el servidor real del proyecto y probada en él:
**Debian 13 (trixie)**, Docker 29.7, nftables 1.1.3.

> Si venís del incendio de hoy: la causa de todo fue tener **dos firewalls**
> peleando y no saber que los puertos de Docker no pasan por donde uno cree.
> Las secciones 3 y 4 son exactamente eso.

---

## Índice

1. [Primer acceso y usuario](#1-primer-acceso-y-usuario)
2. [SSH endurecido](#2-ssh-endurecido)
3. [El modelo mental de red](#3-el-modelo-mental-de-red-lo-más-importante)
4. [Firewall: un solo dueño](#4-firewall-un-solo-dueño)
5. [Docker](#5-docker)
6. [Servicios NATIVOS (Postgres 5432)](#6-servicios-nativos-postgres-5432)
7. [Servicios en contenedor](#7-servicios-en-contenedor)
8. [Portainer](#8-portainer)
9. [nginx-proxy-manager y TLS](#9-nginx-proxy-manager-y-tls)
10. [Registry Docker privado](#10-registry-docker-privado)
11. [fail2ban y actualizaciones](#11-fail2ban-y-actualizaciones)
12. [Backups](#12-backups)
13. [Checklist de verificación](#13-checklist-de-verificación)
14. [Mapa de puertos](#14-mapa-de-puertos)

---

## 1. Primer acceso y usuario

Entrás como `root` con la contraseña del proveedor. Lo primero es dejar de
usarla.

```bash
apt update && apt upgrade -y
apt install -y sudo curl ca-certificates gnupg git
```

> **Nota sobre ufw:** esta guía usa **nftables directo, no ufw**. El motivo
> está en la sección 4. Si el proveedor lo dejó instalado, lo desactivamos.

Creá un usuario propio y sacale la contraseña a root:

```bash
adduser deploy
usermod -aG sudo deploy

# Copiá tu clave pública
mkdir -p /home/deploy/.ssh && chmod 700 /home/deploy/.ssh
echo "ssh-ed25519 AAAA... tu-clave" >> /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh
```

**Antes de cerrar la sesión de root**, abrí otra terminal y comprobá que
`ssh deploy@<ip>` entra. Si no, quedás afuera del servidor.

---

## 2. SSH endurecido

`/etc/ssh/sshd_config.d/99-hardening.conf`:

```
# Sólo clave pública. Sin esto, el 22 abierto a internet recibe fuerza bruta
# constante — se ve en los logs a los minutos de levantar el server.
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes

# root sólo por clave, y preferentemente nunca.
PermitRootLogin prohibit-password

# Higiene
X11Forwarding no
MaxAuthTries 3
LoginGraceTime 30
AllowUsers deploy
```

```bash
sshd -t && systemctl reload ssh     # -t valida ANTES de recargar
```

> `sshd -t` no es opcional: un error de sintaxis en ese archivo y el servicio
> no levanta. Con el 22 caído y sin consola web, el servidor se perdió.

**Cambiar el puerto de SSH no es seguridad**, es sólo menos ruido en los logs.
Si lo hacés, acordate de abrirlo en el firewall *antes* de reiniciar sshd.

---

## 3. El modelo mental de red (lo más importante)

Todo el resto de la guía depende de entender esto.

### Un servicio nativo y un contenedor NO pasan por la misma chain

```
                      ┌─────────────┐
  paquete entrante ──▶│ prerouting  │ (nat: acá ocurre el DNAT de Docker)
                      └──────┬──────┘
                             │
              ¿el destino es el host?
                    │                  │
                   sí                  no (va a un contenedor)
                    ▼                  ▼
              ┌──────────┐       ┌───────────┐
              │  input   │       │  forward  │
              └──────────┘       └───────────┘
              sshd, Postgres      TODO Docker
              nativo, nginx
              del host
```

- **Servicio nativo** (sshd, Postgres instalado con apt) → chain **`input`**.
- **Contenedor con puerto publicado** → chain **`forward`**.

Abrir un puerto en la chain equivocada no hace nada. Ése fue el síntoma de
"sólo anda el 22": era el único servicio no containerizado.

### El DNAT reescribe el puerto ANTES de `forward`

Con `-p 8090:8080`:

```
llega a eth0    dport 8090
nat prerouting  DNAT → 172.18.0.5:8080     ← el puerto cambia acá
chain forward   dport 8080                  ← la regla debe decir 8080
```

**Para contenedores, la regla lleva el puerto INTERNO, no el publicado.**
Una regla con el puerto publicado no matchea nunca.

### Los contenedores también SALEN por `forward`

Si la chain `forward` tiene `policy drop` y sólo reglas de entrada, ningún
contenedor puede llegar a internet: ni a una base remota, ni a una API, ni a
`apt`. El síntoma es una app en crash-loop con timeouts de conexión.

Hace falta, una sola vez:

```
oifname "eth0" ct state new accept
```

---

## 4. Firewall: un solo dueño

### Por qué nftables y no ufw

`ufw` escribe reglas en la tabla `ip filter` (iptables). Muchas imágenes de
VPS traen además una tabla `inet filter` de nftables con `policy drop`.
**Las dos se evalúan sobre el mismo paquete: si cualquiera lo descarta, muere.**

El resultado es un servidor donde `ufw status` dice que el puerto está
permitido y el paquete igual se pierde, sin log ni error. Es exactamente lo que
nos pasó.

**Decisión: nftables es el único firewall.** ufw se desactiva.

```bash
ufw disable
systemctl disable --now ufw
systemctl enable --now nftables
```

### `/etc/nftables.conf` — la única fuente de verdad

Todo lo que esté expuesto tiene que estar en este archivo. Nada de
`nft add rule` suelto: eso vive en memoria y **se pierde al reiniciar**.

```bash
#!/usr/sbin/nft -f

# NO usar `flush ruleset`: además de las nuestras borra las tablas ip/ip6 que
# maneja iptables-nft, que son las de Docker — incluido el DNAT de los puertos
# publicados. Si lo corrés con los contenedores arriba, quedan todos
# inalcanzables hasta que reinicies el daemon (`systemctl restart docker`).
# Recreamos SOLO nuestra tabla: el `add` la crea si no existe, para que el
# `delete` nunca falle.
add table inet filter
delete table inet filter

table inet filter {

    # ─────────────────────────────────────────────────────────────
    # Servicios del HOST (no contenedores)
    # ─────────────────────────────────────────────────────────────
    chain input {
        type filter hook input priority filter; policy drop;

        iif "lo" accept
        ct state established,related accept
        ct state invalid drop

        # ICMP: no lo bloquees. Sin él se rompen el path MTU discovery
        # y todo diagnóstico con ping.
        ip protocol icmp accept
        ip6 nexthdr ipv6-icmp accept

        # SSH
        tcp dport 22 ct state new accept

        # ── Servicios nativos: agregar acá (ver sección 6) ──
        # Postgres nativo SOLO desde IPs conocidas:
        # ip saddr { 203.0.113.10, 198.51.100.20 } tcp dport 5432 ct state new accept

        counter drop
    }

    # ─────────────────────────────────────────────────────────────
    # Contenedores Docker: SIEMPRE el puerto INTERNO
    # ─────────────────────────────────────────────────────────────
    chain forward {
        type filter hook forward priority filter; policy drop;

        ct state established,related accept
        ct state invalid drop

        # Salida de los contenedores a internet.
        # Sin esto ninguna app llega a su base de datos ni a una API externa.
        oifname "eth0" ct state new accept

        # Entrada a los contenedores. La lista es de puertos INTERNOS:
        #   80   nginx-proxy-manager  (publicado 80)
        #   443  nginx-proxy-manager  (publicado 443)
        #   81   nginx-proxy-manager admin (publicado 81) — ver sección 9
        #   9443 Portainer            (publicado 9443)
        #   8080 API                  (publicado 8090)  ← ojo, 8080 no 8090
        tcp dport { 80, 443, 9443, 8080 } ct state new accept

        counter drop
    }

    chain output {
        type filter hook output priority filter; policy accept;
    }
}
```

Aplicar, **siempre validando primero**:

```bash
nft -c -f /etc/nftables.conf && nft -f /etc/nftables.conf && echo "aplicado"
```

`-c` es dry-run. Un error de sintaxis aplicado a ciegas te deja sin red.

### Red de seguridad para no quedarte afuera

Antes de tocar reglas desde SSH (y vale también para el caso de arriba: si te
comés las tablas de Docker, esto te devuelve el estado anterior solo):

```bash
# Si en 5 minutos no cancelás, vuelve al ruleset anterior
nft list ruleset > /root/nft-ok.conf
( sleep 300 && nft -f /root/nft-ok.conf ) &
echo $!    # guardá el PID; si todo salió bien: kill <PID>
```

---

## 5. Docker

Instalación oficial en Debian 13:

```bash
apt install -y ca-certificates curl
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
https://download.docker.com/linux/debian $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
  > /etc/apt/sources.list.d/docker.list

apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
usermod -aG docker deploy
```

### `/etc/docker/daemon.json`

```json
{
  "log-driver": "json-file",
  "log-opts": { "max-size": "10m", "max-file": "3" },
  "live-restore": true
}
```

```bash
systemctl restart docker
```

- **`log-opts`**: sin esto, los logs de un contenedor charlatán llenan el disco
  y tiran el servidor. Es la causa más común de "se quedó sin espacio".
- **`live-restore`**: los contenedores siguen corriendo si reiniciás el daemon.

### Publicá siempre en la interfaz que corresponde

```yaml
ports:
  - "8090:8080"            # 0.0.0.0 — expuesto a internet
  - "127.0.0.1:8090:8080"  # sólo local — accesible por túnel SSH o proxy
```

**Regla práctica:** todo lo que pueda ir detrás del proxy inverso se publica en
`127.0.0.1`. Sólo el proxy (80/443) escucha en `0.0.0.0`.

---

## 6. Servicios NATIVOS (Postgres 5432)

Un servicio instalado con `apt` no pasa por `forward`: se abre en **`input`**.

```bash
apt install -y postgresql-17
```

### 6.1 Decidir el alcance — en orden de preferencia

**a) Sólo local (lo más seguro).** No se abre ningún puerto; se llega por túnel
SSH. Es lo correcto para administrar la base desde tu máquina.

`/etc/postgresql/17/main/postgresql.conf`:
```
listen_addresses = 'localhost'
```

Desde tu máquina:
```bash
ssh -L 5432:localhost:5432 deploy@<ip>
psql -h localhost -U postgres        # como si fuera local
```

**b) Sólo los contenedores.** Que la app en Docker use la base del host:

```
listen_addresses = 'localhost,172.17.0.1'    # gateway de docker0
```

```
# en chain input
iifname "docker0" tcp dport 5432 ct state new accept
```

Y en la app, el host es `host.docker.internal` (con
`extra_hosts: ["host.docker.internal:host-gateway"]`) o directamente
`172.17.0.1`.

**c) Abierto a IPs concretas.** Sólo si un tercero necesita conectarse:

```
listen_addresses = '*'
```

```
# en chain input — NUNCA sin la lista de origen
ip saddr { 203.0.113.10, 198.51.100.20 } tcp dport 5432 ct state new accept
```

> **Nunca `tcp dport 5432 accept` a secas.** Un Postgres abierto a internet es
> escaneado en minutos. Si de verdad tiene que ser público, va con TLS
> (`ssl = on`), `scram-sha-256` y contraseñas fuertes — y aun así conviene
> ponerle delante una VPN (WireGuard) en vez de abrir el puerto.

### 6.2 Autenticación

`/etc/postgresql/17/main/pg_hba.conf`:

```
# TYPE  DATABASE  USER  ADDRESS           METHOD
local   all       all                     peer
host    all       all   127.0.0.1/32      scram-sha-256
host    all       all   172.17.0.0/16     scram-sha-256   # contenedores
# host  all       all   203.0.113.10/32   scram-sha-256   # tercero puntual
```

```bash
systemctl restart postgresql
```

### 6.3 Verificar

```bash
ss -ltnp | grep 5432        # ¿en qué interfaz escucha?
nft list chain inet filter input | grep 5432
```

---

## 7. Servicios en contenedor

La receta, siempre igual:

```bash
# 1. Backup
nft list ruleset > /root/nft-backup-$(date +%F).conf

# 2. Agregar el puerto INTERNO del contenedor a la lista de forward
#    en /etc/nftables.conf, y aplicar
nft -c -f /etc/nftables.conf && nft -f /etc/nftables.conf

# 3. Verificar desde afuera
curl -m 10 http://<ip>:<puerto-publicado>/
```

Si el contenedor tiene que salir a internet, ya está cubierto por la regla
`oifname "eth0"`.

**Pregunta previa a abrir cualquier puerto:** ¿hace falta que esté en internet,
o alcanza con ponerlo detrás del proxy inverso con un subdominio y TLS? Casi
siempre es lo segundo (sección 9).

---

## 8. Portainer

```bash
docker volume create portainer_data

docker run -d \
  --name portainer \
  --restart=always \
  -p 127.0.0.1:9443:9443 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce:lts
```

Fijate en el `127.0.0.1:9443`: **Portainer no va expuesto a internet**. Es una
consola con control total del daemon de Docker; quien entra ahí es root de
todos los contenedores. Se llega por túnel:

```bash
ssh -L 9443:localhost:9443 deploy@<ip>
# y abrís https://localhost:9443
```

Si igual lo querés accesible, que sea **por el proxy con TLS y basic auth**,
nunca como puerto suelto.

> El usuario admin de Portainer hay que crearlo en los primeros minutos: si el
> contenedor queda con la instalación sin inicializar, cualquiera que llegue
> puede reclamarla.

---

## 9. nginx-proxy-manager y TLS

Es lo que evita tener diez puertos sueltos abiertos: un solo par 80/443 y
subdominios hacia cada contenedor.

`/opt/npm/docker-compose.yml`:

```yaml
services:
  npm:
    image: jc21/nginx-proxy-manager:latest
    container_name: nginx-proxy-manager
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
      - "127.0.0.1:81:81"     # admin sólo por túnel
    volumes:
      - ./data:/data
      - ./letsencrypt:/etc/letsencrypt
```

```bash
cd /opt/npm && docker compose up -d
```

Admin por túnel (`ssh -L 81:localhost:81 deploy@<ip>` → `http://localhost:81`).
Credenciales iniciales `admin@example.com` / `changeme`: **cambiálas en el
primer login**.

### Poner un servicio detrás del proxy

1. En el DNS, un registro A del subdominio a la IP del VPS.
2. En NPM: **Proxy Hosts → Add** → dominio, y como destino el **nombre del
   contenedor y su puerto interno** (por ejemplo `papasur-api:8080`).
3. Pestaña **SSL** → *Request a new certificate* + *Force SSL*.

Para que NPM resuelva al contenedor por nombre, los dos tienen que estar en la
misma red de Docker:

```bash
docker network create proxy
docker network connect proxy nginx-proxy-manager
docker network connect proxy papasur-api
```

**Con esto podés cerrar el puerto suelto del servicio**: sacá `8080` de la
lista de `forward` y publicá el contenedor en `127.0.0.1`. Queda un solo
camino de entrada, con TLS y con logs en un solo lugar.

---

## 10. Registry Docker privado

`/opt/registry/docker-compose.yml`:

```yaml
services:
  registry:
    image: registry:3
    container_name: registry
    restart: unless-stopped
    ports:
      - "127.0.0.1:5000:5000"      # detrás del proxy, no suelto
    environment:
      REGISTRY_AUTH: htpasswd
      REGISTRY_AUTH_HTPASSWD_REALM: Registry
      REGISTRY_AUTH_HTPASSWD_PATH: /auth/htpasswd
    volumes:
      - ./data:/var/lib/registry
      - ./auth:/auth
```

Crear las credenciales:

```bash
mkdir -p /opt/registry/auth
docker run --rm --entrypoint htpasswd httpd:2 -Bbn deploy 'una-clave-larga' \
  > /opt/registry/auth/htpasswd
```

Después, un Proxy Host en NPM (`registry.tudominio.com` → `registry:5000`) con
certificado. Del lado del cliente:

```bash
docker login registry.tudominio.com
docker push registry.tudominio.com/miapp:test
```

> **Un registry sin auth y expuesto es una puerta trasera al deploy.**
> Cualquiera puede pushear una imagen con el tag que usás, y Portainer la
> despliega en el próximo re-pull. Si por velocidad lo dejás abierto (como
> hicimos en el hackatón), que sea temporal y con la IP del equipo en el
> `ip saddr` de la regla.

Con TLS real ya no hace falta `insecure-registries` en los clientes.

---

## 11. fail2ban y actualizaciones

```bash
apt install -y fail2ban unattended-upgrades
```

`/etc/fail2ban/jail.local`:

```ini
[DEFAULT]
bantime  = 1h
findtime = 10m
maxretry = 5
# En Debian 13 los logs van a journald, no a /var/log/auth.log
backend  = systemd
# fail2ban usa nftables, coherente con el resto
banaction = nftables-multiport

[sshd]
enabled = true
```

```bash
systemctl enable --now fail2ban
fail2ban-client status sshd
```

Actualizaciones de seguridad automáticas:

```bash
dpkg-reconfigure -plow unattended-upgrades
```

> Con `PasswordAuthentication no`, fail2ban aporta poco contra SSH — nadie
> entra por fuerza bruta sin clave. Sirve igual para bajar el ruido y para los
> servicios web que agregues después.

---

## 12. Backups

Un servidor sin backup probado no tiene backup.

```bash
# Base de datos, todos los días
0 3 * * * pg_dump -U postgres midb | gzip > /var/backups/midb-$(date +\%F).sql.gz

# Volúmenes de Docker
0 4 * * * tar czf /var/backups/volumes-$(date +\%F).tar.gz /var/lib/docker/volumes

# Configuración
0 5 * * * tar czf /var/backups/etc-$(date +\%F).tar.gz /etc/nftables.conf /opt
```

Lo importante no es el cron: es **sacar los backups del servidor** (S3, rsync a
otra máquina) y **restaurar uno** cada tanto para confirmar que sirve.

---

## 13. Checklist de verificación

Después de configurar, comprobá desde **tu máquina**, no desde el servidor:

```bash
# Lo que TIENE que estar abierto
for p in 22 80 443; do
  printf "%-6s " $p; nc -z -w 5 <ip> $p && echo abierto || echo CERRADO
done

# Lo que NO tiene que estarlo
for p in 5432 9443 81 5000 8080; do
  printf "%-6s " $p; nc -z -w 5 <ip> $p && echo "EXPUESTO ⚠" || echo ok
done
```

En el servidor:

```bash
ss -ltnp                              # qué escucha y en qué interfaz
nft list ruleset | grep -E "dport|policy"
docker ps --format '{{.Names}}\t{{.Ports}}'
sshd -t && echo "ssh config ok"
nft -c -f /etc/nftables.conf && echo "firewall config ok"
```

**Y la prueba que de verdad importa: reiniciá el servidor** y volvé a correr el
checklist. Si algo cambia, es que estaba sólo en memoria.

```bash
reboot
```

---

## 14. Mapa de puertos

| Publicado | Interno | Servicio | Chain | Expuesto |
| --- | --- | --- | --- | --- |
| 22 | — | sshd (host) | `input` | sí |
| 80 | 80 | nginx-proxy-manager | `forward` | sí |
| 443 | 443 | nginx-proxy-manager | `forward` | sí |
| 81 | 81 | NPM admin | `forward` | no — túnel |
| 9443 | 9443 | Portainer | `forward` | no — túnel |
| 5000 | 5000 | registry | `forward` | no — vía proxy |
| 8090 | **8080** | API | `forward` | según caso |
| 5432 | — | Postgres nativo | `input` | no — túnel o IPs |

**La columna que se usa para escribir la regla es `Interno` en contenedores y
`Publicado` en servicios nativos.** Confundirlas es el error que más caro sale.

---

## Resumen de las cinco reglas

1. **Un solo firewall.** nftables, y ufw desactivado.
2. **Un solo archivo.** `/etc/nftables.conf`. `nft add rule` es para probar, no
   para dejar.
3. **`input` para lo nativo, `forward` para Docker** — y en Docker, el puerto
   **interno**.
4. **Todo lo administrativo por túnel SSH**, no por puerto abierto.
5. **Validá antes de aplicar** (`nft -c`, `sshd -t`) y **reiniciá para
   verificar**.
