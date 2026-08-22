# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack & convenciones

- **.NET 10 LTS / C#** (nullable + implicit usings, centralizados en `Directory.Build.props`; versiones de paquetes en `Directory.Packages.props` — CPM), **PostgreSQL 17** vía EF Core (Npgsql), Docker Compose para runtime, Serilog para logging.
- Código y docs en **español**; commits cortos en inglés estilo Conventional Commits (`feat(scope): ...`).

## Comandos

```bash
dotnet build                              # compilar
dotnet test Papasur.Tests --filter "FullyQualifiedName!~Integration"   # unitarios (sin DB)
dotnet test Papasur.Tests --filter "FullyQualifiedName~Integration"    # integración (requiere Docker)
dotnet run --project Papasur.Api      # API local; OpenAPI en /openapi/v1.json (solo dev)
docker compose up                         # dev: API hot-reload + Postgres 17
```

## Arquitectura

Clean/onion, dependencias hacia adentro: `Domain` (entidades puras) ← `Application` (CQRS + ports) ← `Infrastructure` (EF, repos `Ef*`) ← `Api` (controllers, DI en `Program.cs`).

- **CQRS propio, sin MediatR**: interfaces en `Application/Abstractions/Messaging`. Un handler por operación, registrado explícitamente en `Application/DependencyInjection.cs`. Los controllers inyectan handlers directo.
- **Result pattern** (`Application/Abstractions/Result.cs`): errores de negocio esperables → `Result.Failure(Error)` y el controller los mapea a ProblemDetails 4xx. Las excepciones quedan para lo inesperado (`GlobalExceptionHandler` → 500 ProblemDetails sin filtrar detalles).
- **Feature nueva**: command/query + handler en `Application/<Feature>/`, port en `Application/<Feature>/Ports/`, impl `Ef*Repository` en `Infrastructure/<Feature>/`, registrar en ambos `DependencyInjection.cs`, controller en `Api/Controllers/`. La feature `Items` es el ejemplo del patrón.

## Migraciones (reglas)

- **Un solo** `AppDbContext`; migraciones **SOLO** en `Papasur.Infrastructure/Persistence/Migrations` (no crear otras carpetas/contextos). snake_case por convención.
- `dotnet tool restore` para el `dotnet-ef` pineado. Alta: `dotnet ef migrations add <Nombre> --context AppDbContext --project Papasur.Infrastructure --startup-project Papasur.Api --output-dir Persistence/Migrations`.
- Auto-migrate al arrancar con `Ef__AutoMigrate` (ON por defecto en Development). Para prod, preferir script idempotente: `dotnet ef migrations script --idempotent`.

## Seguridad / runtime (configurado en `Program.cs`)

- **JWT** HS256: `Jwt__SymmetricKey` (≥32 bytes, obligatoria fuera de Development), `Jwt__Issuer`/`Jwt__Audience`. Policy `Admin` (rol `admin`). Ejemplo protegido: `GET /api/v1/me`.
- **CORS** por env: `Cors__AllowedOrigins__0..N` (fail-closed fuera de dev; en dev permite localhost).
- **Rate limiting** global por IP (`RateLimiting__PermitLimit` / `WindowSeconds`), `SecurityHeadersMiddleware`, `/health` chequea Postgres (503 si está caído).
- Secrets SIEMPRE por `.env`/env vars (ver `.env.example`), nunca en appsettings ni en git.

## CI

- `ci.yml`: build + tests (unit + integration) en PRs y pushes a `main`.
- `build-and-push.yml`: deploy por rama (`ae/test`/`ae/beta` → imagen `:test`/`:beta` al registry `DOCKER_REGISTRY`). `main` no deploya.
- `guard-merge-direction.yml`: las ramas de entorno nunca son origen de merges (flujo `feature → main → ae/*`).
