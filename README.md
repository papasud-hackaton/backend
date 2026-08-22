# Papasur — Backend

API base. **.NET 10 (LTS)** + **PostgreSQL 17** vía **EF Core (Npgsql)**, con **Clean Architecture**, **CQRS** y hardening de producción (JWT, CORS, rate limiting, ProblemDetails, Serilog, health checks).

## Arquitectura

Clean/onion en cuatro proyectos (las dependencias apuntan hacia adentro):

- **`Papasur.Domain`** — entidades, sin dependencias.
- **`Papasur.Application`** — CQRS: commands/queries + handlers (uno por operación), **ports** (interfaces en `*/Ports/`) y **Result pattern** (`Abstractions/Result.cs`). Sin referencias a EF/infraestructura. Registro explícito en `DependencyInjection.cs` (`AddApplication()`).
- **`Papasur.Infrastructure`** — repositorios EF (`Ef*Repository` implementando los ports), `AppDbContext`, migraciones. Wired en `DependencyInjection.cs` (`AddInfrastructure()`).
- **`Papasur.Api`** — controllers, middleware (excepciones → ProblemDetails, security headers), composición DI en `Program.cs`.

Propiedades comunes en `Directory.Build.props`; versiones de paquetes centralizadas en `Directory.Packages.props` (CPM); estilo en `.editorconfig`.

### CQRS + Result

Sin MediatR (evita el licenciamiento comercial): abstracciones propias en `Application/Abstractions/Messaging`. Los errores de negocio esperables se devuelven como `Result.Failure(Error)` (el controller los mapea a ProblemDetails 4xx); las excepciones quedan para lo inesperado (`GlobalExceptionHandler` → 500 sin filtrar detalles). Al agregar una feature:

1. Command/Query + handler en `Application/<Feature>/Commands|Queries/...`
2. Port en `Application/<Feature>/Ports/` + implementación `Ef*` en `Infrastructure/<Feature>/`
3. Registrar handler en `Application/DependencyInjection.cs` y repo en `Infrastructure/DependencyInjection.cs`
4. Controller en `Api/Controllers/`

La feature `Items` (entidad `Item`) queda como ejemplo del patrón completo — reemplazarla por el dominio real.

## Comandos

```bash
dotnet build                              # compilar la solución
dotnet test Papasur.Tests --filter "FullyQualifiedName!~Integration"  # unitarios (sin DB)
dotnet test Papasur.Tests --filter "FullyQualifiedName~Integration"   # integración (Testcontainers, requiere Docker)
dotnet run --project Papasur.Api      # API local (aplica migraciones en Development)

# Con Docker (dev: hot reload + Postgres 17, docker-compose.override.yml automático)
docker compose up
```

## Migraciones (registro limpio)

**Un solo** `AppDbContext`, **una sola** carpeta de migraciones (`Papasur.Infrastructure/Persistence/Migrations`), un solo `__EFMigrationsHistory` en schema `public`. Naming convention **snake_case**. `dotnet-ef` está pineado como tool local (`.config/dotnet-tools.json` → `dotnet tool restore`).

```bash
export ConnectionStrings__pg='Host=localhost;Port=5432;Database=papasur;Username=postgres;Password=...;SSL Mode=Disable'

# Nueva migración (SIEMPRE en Persistence/Migrations — no crear carpetas nuevas)
dotnet ef migrations add <Nombre> --context AppDbContext \
  --project Papasur.Infrastructure --startup-project Papasur.Api \
  --output-dir Persistence/Migrations

# Aplicar
dotnet ef database update --context AppDbContext \
  --project Papasur.Infrastructure --startup-project Papasur.Api

# SQL idempotente (preferido para staging/prod — revisar antes de aplicar)
dotnet ef migrations script --idempotent -c AppDbContext \
  --project Papasur.Infrastructure --startup-project Papasur.Api -o sql/<nombre>.sql
```

- Auto-migrate al arrancar: `Ef:AutoMigrate` (env `Ef__AutoMigrate`); ON por defecto en Development.

## Seguridad / runtime

- **JWT** HS256: `Jwt__SymmetricKey` (≥32 bytes, obligatoria fuera de Development), `Jwt__Issuer`/`Jwt__Audience`; policy `Admin` (rol `admin`); ejemplo protegido `GET /api/v1/me`.
- **CORS** por env (`Cors__AllowedOrigins__0..N`): fail-closed fuera de dev; en dev permite localhost.
- **Rate limiting** global por IP (`RateLimiting__PermitLimit`/`WindowSeconds`) con 429; `SecurityHeadersMiddleware`.
- **`/health`** chequea Postgres (503 si está caído) — usable como healthcheck de contenedor/LB.
- **Serilog** estructurado a consola + request logging (config en `appsettings` sección `Serilog`).

## Docker & CI

- `docker-compose.yml` = imagen base test/CI (target `final`, self-contained). `docker-compose.override.yml` = dev (target `dev`, `dotnet watch`, repo montado, **Postgres 17** con healthcheck).
- `ci.yml`: build + tests unitarios + tests de integración (Testcontainers) en PRs y pushes a `main` (exigirlo en branch protection).
- `build-and-push.yml`: push a `ae/test`/`ae/beta` → corre tests → buildea y pushea `papasur/api:test|beta` al registry definido en `DOCKER_REGISTRY`. `main` es integración, no deploya. **TODO**: setear el registry real en el workflow, compose y `.env`.
- `guard-merge-direction.yml`: las ramas de entorno sólo reciben merges (flujo `feature → main → ae/test|ae/beta`).
- `dependabot.yml`: NuGet + GitHub Actions semanal.

## Configuración

Secrets en `.env` (gitignoreado; `.env.example` documenta las claves). Nunca en appsettings ni en git.
