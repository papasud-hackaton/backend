# Flujogramas de red y firewall

Tres diagramas: por dónde pasa un paquete, qué regla escribir para exponer algo,
y cómo diagnosticar cuando no llega. Complementan
[VPS-SETUP.md](VPS-SETUP.md).

---

## 1. Camino de un paquete entrante

Es el diagrama que explica por qué abrir un puerto "obvio" no funciona.

```mermaid
flowchart TD
    A[Paquete entra por eth0] --> B[nat prerouting]
    B --> C{¿Hay DNAT de Docker<br/>para ese puerto?}

    C -->|No: es del host| D[chain INPUT]
    C -->|Sí: va a un contenedor| E[DNAT reescribe destino<br/>IP y PUERTO del contenedor]

    D --> D1{¿Hay regla<br/>en input?}
    D1 -->|Sí| D2[Llega a sshd / Postgres nativo]
    D1 -->|No| D3[DROP por policy]

    E --> F[chain FORWARD]
    F --> F1{¿Hay regla para el<br/>puerto INTERNO?}
    F1 -->|Sí| F2[Llega al contenedor]
    F1 -->|No| F3[DROP por policy]

    style E fill:#4a3d00,stroke:#c9a227,color:#fff
    style F1 fill:#4a3d00,stroke:#c9a227,color:#fff
    style D3 fill:#4a1010,stroke:#c94141,color:#fff
    style F3 fill:#4a1010,stroke:#c94141,color:#fff
```

**Lo que hay que retener:** el DNAT ocurre **antes** de `forward`, así que para
`-p 8090:8080` la regla se escribe sobre el **8080**. Una regla con el 8090 no
matchea nunca.

---

## 2. Quiero exponer algo: ¿qué regla escribo?

```mermaid
flowchart TD
    A[Quiero exponer un servicio] --> B{¿Corre en Docker?}

    B -->|No, es nativo<br/>apt install| C[chain INPUT<br/>con el puerto REAL]
    B -->|Sí, contenedor| D{¿Puede ir detrás del<br/>proxy inverso?}

    C --> C1{¿Lo necesita<br/>todo internet?}
    C1 -->|No| C2["listen_addresses = 'localhost'<br/>+ túnel SSH<br/>SIN regla de firewall"]
    C1 -->|Sí, pero sólo algunos| C3["ip saddr { IPs } tcp dport N accept"]
    C1 -->|Sí, público| C4["tcp dport N accept<br/>+ TLS y auth fuertes"]

    D -->|Sí, lo normal| E["Publicar en 127.0.0.1<br/>Proxy Host en NPM + TLS<br/>SIN abrir puerto nuevo"]
    D -->|No: no es HTTP,<br/>o hace falta el puerto crudo| F["chain FORWARD<br/>con el puerto INTERNO"]

    F --> G{¿El contenedor necesita<br/>salir a internet?}
    G -->|Sí| H["Ya está cubierto por<br/>oifname eth0 accept"]
    G -->|No| I[Listo]

    E --> J[Persistir en /etc/nftables.conf]
    C2 --> J
    C3 --> J
    C4 --> J
    H --> J
    I --> J

    J --> K["nft -c -f /etc/nftables.conf<br/>validar ANTES de aplicar"]
    K --> L[nft -f /etc/nftables.conf]
    L --> M[Verificar desde AFUERA<br/>y reiniciar para confirmar]

    style E fill:#0f3d2e,stroke:#2ea36b,color:#fff
    style C2 fill:#0f3d2e,stroke:#2ea36b,color:#fff
    style K fill:#4a3d00,stroke:#c9a227,color:#fff
    style C4 fill:#4a1010,stroke:#c94141,color:#fff
```

El camino verde es el preferido: **nada nuevo abierto**. El rojo es el último
recurso.

---

## 3. No llega: cómo encontrar dónde muere

Cada paso descarta una capa. En orden, del más barato al más caro.

```mermaid
flowchart TD
    A[El servicio no responde<br/>desde afuera] --> B["nc -z IP PUERTO"]
    B -->|Abre| C[No es red:<br/>mirar la app]
    B -->|Timeout| D["docker ps · docker logs<br/>curl localhost desde el server"]

    D -->|No responde ni adentro| E[La app está caída<br/>o en crash-loop]
    E --> E1{¿El log dice<br/>timeout de conexión?}
    E1 -->|Sí| E2["Falta la salida:<br/>oifname eth0 accept"]
    E1 -->|No| E3[Es un bug de la app]

    D -->|Responde adentro| F["tcpdump -ni any tcp port N<br/>and host TU_IP"]

    F -->|No aparece nada| G[Bloqueo AGUAS ARRIBA:<br/>firewall del proveedor]
    F -->|SYN sin SYN-ACK| H[Lo descartás vos]

    H --> I{¿Aparece en el log<br/>de UFW BLOCK?}
    I -->|Sí| J[Es ufw:<br/>revisar chain input]
    I -->|No| K["nft list chain inet filter forward"]

    K --> L{¿Está el puerto<br/>INTERNO en la lista?}
    L -->|No| M[Agregar la regla<br/>con el puerto interno]
    L -->|Sí| N["tcpdump en el bridge:<br/>tcpdump -ni br-xxxx"]

    N -->|No llega al bridge| O["Falta el DNAT:<br/>systemctl restart docker"]
    N -->|Llega al bridge| P[El contenedor no escucha<br/>en 0.0.0.0 adentro]

    style F fill:#4a3d00,stroke:#c9a227,color:#fff
    style E2 fill:#0f3d2e,stroke:#2ea36b,color:#fff
    style M fill:#0f3d2e,stroke:#2ea36b,color:#fff
    style O fill:#0f3d2e,stroke:#2ea36b,color:#fff
```

### El paso que más ahorra: el tcpdump

```bash
timeout 25 tcpdump -ni any tcp port 8090 and host $(curl -s ifconfig.me) -c 6
```

- **Nada** → el bloqueo está antes de tu máquina.
- **SYN repetidos sin respuesta** → llegan y los estás tirando vos.

Los tres casos reales de este proyecto salieron de acá:

| Síntoma | Causa | Arreglo |
| --- | --- | --- |
| Registry no aceptaba push | `forward` en policy drop sin reglas | `tcp dport 5000 accept` |
| API colgada en 8090 | la regla decía 8090, el DNAT ya lo había pasado a 8080 | `tcp dport 8080 accept` |
| API en crash-loop | los contenedores no podían salir | `oifname "eth0" accept` |
| Todo caído tras aplicar el firewall | `flush ruleset` borró el DNAT de Docker | `systemctl restart docker` y no volver a usar `flush ruleset` |

---

## 4. Publicar una versión nueva

```mermaid
flowchart LR
    A[git push] --> B["./scripts/publish-image.sh test"]
    B --> C{¿El registry<br/>es accesible?}
    C -->|Sí| D[docker push directo]
    C -->|No| E[docker save + SSH<br/>y push desde adentro]
    D --> F[Portainer: Update the stack<br/>+ Re-pull image]
    E --> F
    F --> G{¿Coinciden los digests<br/>local y del registry?}
    G -->|Sí| H[Desplegado]
    G -->|No| I[El re-pull no pasó:<br/>repetir con Re-pull tildado]

    style F fill:#4a3d00,stroke:#c9a227,color:#fff
    style I fill:#4a1010,stroke:#c94141,color:#fff
```

Sin el **Re-pull image**, Portainer sigue corriendo la imagen vieja. Con la base
ya migrada, el código viejo falla con `errorMissingColumn` y todo devuelve 500.
