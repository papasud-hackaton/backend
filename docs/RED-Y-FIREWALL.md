# Red y firewall del VPS — guía

Cómo exponer un servicio en este servidor y, sobre todo, **por qué las reglas
obvias no funcionan**. Está escrito a partir de tres días de síntomas reales:
el registry que no aceptaba push, la API que quedaba colgada y el contenedor
que no podía llegar a la base de datos.

Servidor: `2.24.86.109` (`srv1653314`), Ubuntu con Docker, Portainer y
nginx-proxy-manager.

---

## 1. Lo que hay que entender primero

### Hay DOS firewalls corriendo a la vez

```
ufw        (iptables)  -> reglas de INPUT
nftables   (inet filter) -> chains input / forward / output, policy drop
```

**Los dos se evalúan sobre el mismo paquete. Si cualquiera lo tira, el paquete
muere.** Por eso `ufw allow 5000/tcp` no alcanzaba: ufw lo permitía y nftables
lo seguía descartando en silencio.

Ver el que manda:

```bash
nft list table inet filter
```

### Los puertos de Docker NO pasan por `input`, pasan por `forward`

Un servicio del host (sshd) entra por la chain `input`. Un contenedor con
puerto publicado recibe el tráfico **DNAT-eado hacia la IP del contenedor**, y
eso atraviesa `forward`.

Esto explica el síntoma que más tiempo costó: **el único puerto que funcionaba
era el 22**, el único servicio que no es un contenedor.

### El DNAT reescribe el puerto ANTES de `forward`

Esta es la trampa fina. Con `-p 8090:8080`:

```
llega a eth0   -> dport 8090
nat prerouting -> DNAT a 172.x.x.x:8080     <-- acá cambia el puerto
chain forward  -> dport 8080                <-- la regla tiene que decir 8080
```

Una regla `tcp dport 8090 accept` **no matchea nunca**. Hay que abrir el
**puerto interno del contenedor**.

Por eso el front funcionaba y la API no: el front publica `8095:80` y el 80 ya
estaba permitido de antes; la API publicaba `8090:8080` y el 8080 no.

### El tráfico SALIENTE de los contenedores también pasa por `forward`

Con `policy drop` y solo reglas de entrada, **los contenedores no pueden salir
a internet**. Ese fue el motivo de que la API entrara en crash-loop: resolvía
el host de Render pero el SYN a Postgres moría en la propia máquina.

---

## 2. Receta: exponer un servicio nuevo

Supongamos un contenedor publicado como `-p 9000:3000`.

```bash
# 1. Backup, siempre
nft list ruleset > /root/nft-backup-$(date +%F).conf

# 2. Abrir el PUERTO DEL CONTENEDOR (3000), no el publicado (9000)
nft add rule inet filter forward tcp dport 3000 ct state new accept

# 3. Verificar desde afuera
curl -m 10 http://2.24.86.109:9000/
```

Si el servicio necesita salir a internet (base de datos remota, API externa),
esto tiene que estar una sola vez:

```bash
nft add rule inet filter forward oifname "eth0" ct state new accept
```

`ufw` no hay que tocarlo para contenedores: no interviene en `forward`.

---

## 3. Persistencia — el paso que se olvida

`nft add rule` vive **solo en memoria**. Al reiniciar se pierde todo y el
sistema vuelve a estar caído exactamente igual.

El ruleset se carga de `/etc/nftables.conf` (servicio `nftables`, enabled).
Hay que dejar la chain `forward` así:

```
    chain forward {
        type filter hook forward priority 0;
        policy drop;

        # Vuelta de conexiones ya establecidas (incluye contenedores).
        ct state established,related accept

        # Salida de los contenedores a internet (Postgres en Render, etc.).
        oifname "eth0" ct state new accept

        # Puertos INTERNOS de los contenedores publicados:
        #   registry  5000:5000
        #   nginx-proxy-manager  80:80 / 443:443
        #   portainer 9443:9443
        #   front     8095:80
        #   API       8090:8080   <-- por eso va el 8080
        tcp dport { 80, 443, 5000, 8080, 9443 } ct state new accept
    }
```

Validar **antes** de confiar en el archivo:

```bash
nft -c -f /etc/nftables.conf && echo "sintaxis OK"
```

`-c` es dry-run: chequea sin aplicar. Si tiene un error de sintaxis y se
reinicia, el servidor arranca sin firewall o sin red.

> **Estado actual: el archivo está desactualizado.** Sólo tiene la regla del
> 5000. Todo lo demás (80/443/9443, 8080, y la salida por `eth0`) está
> únicamente en memoria. **Al primer reboot se cae todo otra vez.** Copiar el
> bloque de arriba a `/etc/nftables.conf` es lo que cierra el tema.

---

## 4. Diagnóstico: cómo encontrar dónde muere un paquete

En orden, del más barato al más caro. Cada paso descarta una capa.

### 4.1 ¿Está abierto el puerto?

```bash
nc -z -w 6 2.24.86.109 8090 && echo abierto || echo bloqueado
```

### 4.2 ¿El contenedor está sano y escuchando?

```bash
docker ps --format '{{.Names}}|{{.Status}}|{{.Ports}}'
docker logs --tail 30 <contenedor>
curl -m 5 http://localhost:8090/health     # desde el propio server
```

Si responde adentro pero no afuera, el problema es de red, no de la app.

### 4.3 ¿Llegan los paquetes a la máquina?

**La prueba decisiva.** Distingue "me bloquea el proveedor" de "me bloqueo yo":

```bash
timeout 25 tcpdump -ni any tcp port 8090 and host <TU_IP_PUBLICA> -c 6
```

- **No aparece nada** → el bloqueo está aguas arriba (proveedor / red).
- **Aparecen SYN sin SYN-ACK** → llegan y los estás descartando vos. Es esto
  el 99 % de las veces.

Tu IP pública: `curl -s ifconfig.me`.

### 4.4 ¿Quién los descarta?

```bash
# ufw loguea lo que bloquea
journalctl -k --since "-3min" | grep "UFW BLOCK"

# nftables: si no hay log de ufw pero el paquete muere, es nftables
nft list chain inet filter forward
```

Truco útil: mirar si el paquete llega al bridge del contenedor.

```bash
tcpdump -ni br-xxxxxxxx tcp port 8080 -c 5
```

Si llega a `eth0` pero **no** al bridge, murió en `forward`.

### 4.5 Falsos culpables que ya descartamos

- **fail2ban**: tiene sólo la jail `sshd` y cero baneos.
- **Firewall del proveedor**: los paquetes llegan a `eth0`, así que no filtra
  nada en esos puertos.
- **iptables/Docker**: la chain `DOCKER` tiene su ACCEPT correcto y los
  contadores suben. El problema siempre estuvo en la tabla `inet filter` de
  nftables, que es otra y se evalúa igual.

---

## 5. El registry

```
http://2.24.86.109:5000     sin TLS y sin autenticación
```

Publicar una imagen:

```bash
./scripts/publish-image.sh test
```

El script detecta si el registry es accesible: si lo es hace `docker push`
directo, y si no manda la imagen por SSH y la pushea desde adentro del server.
Cuando el puerto está abierto, **los devs no necesitan acceso SSH**.

Cada máquina que pushee necesita esto en Docker (Settings → Docker Engine, o
`/etc/docker/daemon.json`), y reiniciar Docker:

```json
{ "insecure-registries": ["2.24.86.109:5000"] }
```

Ver qué hay publicado:

```bash
curl -s http://2.24.86.109:5000/v2/_catalog
curl -s http://2.24.86.109:5000/v2/papasur/api/tags/list
```

> **El registry está abierto al público sin autenticación.** Cualquiera con la
> IP puede pullear **y pushear** — y lo que se pushea es lo que Portainer
> despliega. Es aceptable para el hackatón; para algo real, va detrás del
> nginx-proxy-manager con TLS y basic auth, o con el `Source` de la regla
> limitado a las IPs del equipo.

---

## 6. Después de publicar: el paso que rompe deploys

Portainer **no** repullea solo. Si actualizás la imagen y no lo forzás, sigue
corriendo la vieja — y con la base ya migrada al esquema nuevo, el código viejo
falla con `errorMissingColumn` y todo devuelve 500.

**Portainer → Stacks → *(el stack)* → Update the stack → tildar "Re-pull image".**

Para verificar que quedó la imagen correcta:

```bash
# en el server
docker inspect papasur-api --format '{{.Image}}'

# y comparar con el registry
curl -s -H 'Accept: application/vnd.oci.image.index.v1+json' -D - -o /dev/null \
  http://2.24.86.109:5000/v2/papasur/api/manifests/test | grep -i docker-content-digest
```

Si los digests no coinciden, el re-pull no pasó.

---

## 7. Mapa de puertos

| Publicado | Interno | Servicio | Regla en `forward` |
| --- | --- | --- | --- |
| 22 | — | sshd (host) | va por `input`, no por `forward` |
| 80 / 443 | 80 / 443 | nginx-proxy-manager | `dport { 80, 443 }` |
| 5000 | 5000 | registry Docker | `dport 5000` |
| 8090 | **8080** | API | `dport 8080` |
| 8095 | **80** | front | ya cubierto por `dport 80` |
| 9443 | 9443 | Portainer | `dport 9443` |

La columna que importa para escribir la regla es **Interno**.
