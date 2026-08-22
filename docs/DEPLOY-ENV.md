# Deploy — variables de entorno

Todo lo que hay que cargar para levantar el sistema. Ningún secreto vive en el
repo: van en el `.env` local (gitignoreado) o en la sección *Environment
variables* de Portainer.

---

## 1. Backend — stack de Portainer

Stack: [portainer-stack.yml](../portainer-stack.yml).
Imagen: `localhost:5000/papasur/api:test` (el registry corre en el mismo host).

| Variable | Valor | Obligatoria |
| --- | --- | --- |
| `DOCKER_REGISTRY` | `localhost:5000` | sí |
| `API_IMAGE_TAG` | `test` | sí |
| `API_PORT` | `8090` | no (default 8090) |
| `PG_CONN` | connection string de Postgres | **sí** |
| `EF_AUTOMIGRATE` | `true` | no (default true) |
| `JWT_KEY` | clave HS256, **≥ 32 bytes** | **sí** |
| `JWT_ISSUER` / `JWT_AUDIENCE` | `papasur` | no |
| `JWT_EXPIRATION_MINUTES` | `480` (8 h) | no |
| `SEED_ADMIN_EMAIL` | `admin@papasud.com` | sí en el primer arranque |
| `SEED_ADMIN_PASSWORD` | contraseña del admin inicial | sí en el primer arranque |
| `CORS_ORIGIN_0` | URL del front, p. ej. `https://papasud.tudominio.com` | **sí en producción** |
| `CORS_ORIGIN_1` | segundo origen, si hay | no |
| `RATE_LIMIT_PERMITS` | `100` | no |
| `RATE_LIMIT_WINDOW_SECONDS` | `10` | no |

### Connection string de Render

```
Host=dpg-da4gba8n74is73dic400-a.oregon-postgres.render.com;Port=5432;Database=papasud;Username=papausr;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true
```

El `SSL Mode=Require` no es opcional: Render rechaza conexiones sin TLS.

### Generar la clave JWT

```bash
openssl rand -base64 48
```

Fuera de Development **la app no arranca sin ella**. Es a propósito: un backend
firmando tokens con una clave de ejemplo es peor que un backend caído.

### CORS

Si `CORS_ORIGIN_0` queda vacío fuera de Development, la política es
**fail-closed**: el navegador va a bloquear al front. En Development se permite
cualquier `localhost` sin configurar nada.

---

## 2. Front

El front lee la config en dos pasos: primero `window.__PAPASUD_CONFIG__` que
escribe el contenedor al arrancar (`docker/40-runtime-config.sh`), y si no, las
`VITE_*` congeladas en el bundle.

### Desarrollo — `.env.local`

```bash
VITE_API_URL=http://localhost:8080/api/v1
VITE_USE_MOCKS=false
```

### Contenedor

```bash
API_URL=https://<host-del-backend>/api/v1
USE_MOCKS=false
```

Dos cosas que rompen si se pasan por alto:

- **La URL incluye `/api/v1`.** Sin eso, todas las llamadas dan 404.
- **Los mocks se apagan con el string exacto `false`.** Cualquier otro valor
  (vacío, `0`, `no`) los deja prendidos y el front no toca el backend.

---

## 3. Primer arranque

1. Cargar las variables en Portainer y desplegar el stack.
2. Al arrancar, la API aplica las migraciones pendientes y las loguea.
3. Si la tabla de usuarios está vacía, crea el admin con `SEED_ADMIN_EMAIL` /
   `SEED_ADMIN_PASSWORD`. **Solo si está vacía**: cambiar esas variables después
   no cambia la contraseña.
4. Verificar:

```bash
curl https://<host>/health
curl -X POST https://<host>/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@papasud.com","password":"<la-que-pusiste>"}'
```

El login devuelve `{ user, token, expiresAt }`. Con ese token, todo lo demás.

---

## 4. Publicar una versión nueva

```bash
./scripts/publish-image.sh test
```

Buildea, detecta si el registry es accesible y publica (directo o por SSH).
Después: **Portainer → Stacks → papasur → Update the stack → "Re-pull image"**.

---

## 5. Correo

`IInvitationSender` hoy tiene una implementación que **loguea** el enlace de
invitación y de recuperación en vez de mandarlo. Para la demo alcanza: el token
sale por `docker compose logs`. Para producción hay que registrar una
implementación SMTP en `Infrastructure/DependencyInjection.cs` — es una línea, el
puerto ya está.
