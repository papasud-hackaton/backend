# ai.md — Rules & Conventions

Working agreement for anyone (human or AI assistant) writing code in this repository.
Read this before touching the codebase.

---

## 1. Product premise

**Papasud — documentation copilot for export.**

We are building an assistant that generates **proforma invoices and export
documentation** for seed potato exports. Two things drive the design:

- **Fast capture.** Documents must be creatable by **dictation** or by **quick
  selection of traceability data** — not by filling long forms by hand.
- **Pre-filled by inference.** The system reads the **documentary requirements**
  (from sample forms and regulations provided by Papasud) and cross-references
  them with the **traceability data of a specific lot**, so every field that is
  already known is pre-completed. The user only reviews and fills the gaps.

Inputs provided by the business: document templates and the requirements
demanded by the control agencies that regulate seed export.

**Implications for the code**

- A **lot** (`lote`) and its traceability data are the center of the domain.
  Document generation is a projection over that data, never the source of truth.
- Documentary requirements are **data, not code**. They change per agency and per
  destination country; model them as configurable rules/templates that can be
  updated without a redeploy.
- Every generated document must be **traceable back to its inputs**: which lot,
  which template version, which values were inferred vs. entered by a human.
- Inference is **assistive**: a pre-filled field is a suggestion. Nothing is ever
  submitted to an agency without an explicit human confirmation step.

This section will grow as the scope is defined. Keep it the single source of
truth for product intent.

---

## 2. Clean Architecture

Onion layers, dependencies always point **inwards**:

```
Domain  <-  Application  <-  Infrastructure  <-  Api
```

| Project | Contains | May reference |
| --- | --- | --- |
| `Papasur.Domain` | Entities, value objects, domain rules. Pure C#. | nothing |
| `Papasur.Application` | Commands/queries + handlers, DTOs, **ports** (interfaces), `Result`. | Domain |
| `Papasur.Infrastructure` | EF Core, `AppDbContext`, `Ef*Repository`, external services. | Domain, Application |
| `Papasur.Api` | Controllers, middleware, DI composition in `Program.cs`. | Application, Infrastructure |

Rules:

- **The Domain has no dependencies.** No EF attributes, no `DbContext`, no
  framework types, no external SDKs. If a domain rule needs data it does not
  own, it is the wrong place for that rule.
- **Application defines ports, Infrastructure implements them.** Interfaces live
  in `Application/<Feature>/Ports/`; the implementation is
  `Infrastructure/<Feature>/Ef<Name>Repository.cs`. Application never references
  EF Core.
- **The Api layer is thin.** Controllers validate the shape of the request, call
  a handler, and map the outcome to HTTP. No business logic, no direct database
  access.
- **No leaking of entities to the outside.** Queries return DTOs, not entities.

## 3. CQRS

Hand-rolled CQRS — **no MediatR** (avoids its commercial licence). Abstractions
live in `Application/Abstractions/Messaging`.

- **One handler per operation.** `ICommand`/`ICommandHandler` for writes,
  `IQuery`/`IQueryHandler` for reads. A handler does one thing; do not add a
  second responsibility to an existing one.
- **Explicit DI registration.** Every handler is registered by hand in
  `Application/DependencyInjection.cs`; every repository in
  `Infrastructure/DependencyInjection.cs`. No assembly scanning — if it is not
  registered, it does not exist.
- **Controllers inject handlers directly.** No service layer in between.
- **Result pattern** (`Application/Abstractions/Result.cs`): expected business
  failures return `Result.Failure(Error)` and the controller maps them to a 4xx
  `ProblemDetails`. Exceptions are for the *unexpected* only —
  `GlobalExceptionHandler` turns those into a 500 without leaking internals.
- Commands and queries are `record` types; handlers take a `CancellationToken`
  and pass it down.

**Adding a feature** (the `Users` feature is the reference implementation —
entity, port, EF repository, command + query handlers, controller and tests):

1. Command/query + handler in `Application/<Feature>/Commands|Queries/...`
2. Port in `Application/<Feature>/Ports/`
3. `Ef*Repository` implementation in `Infrastructure/<Feature>/`
4. Register both in the two `DependencyInjection.cs` files
5. Controller in `Api/Controllers/`
6. Unit test for the handler; integration test for the repository

## 4. Persistence & migrations

- **One `AppDbContext`. One migrations folder:**
  `Papasur.Infrastructure/Persistence/Migrations`. Never create a second context
  or a second folder.
- snake_case naming convention; a single `__EFMigrationsHistory` in schema
  `public`.
- `dotnet-ef` is pinned as a local tool — `dotnet tool restore` first.

```bash
dotnet ef migrations add <Name> --context AppDbContext \
  --project Papasur.Infrastructure --startup-project Papasur.Api \
  --output-dir Persistence/Migrations
```

- Auto-migrate on startup is controlled by `Ef__AutoMigrate` (on by default in
  Development). For production prefer a reviewed idempotent script:
  `dotnet ef migrations script --idempotent`.

## 5. Docker

- **`docker compose up` is the development entry point.**
  `docker-compose.override.yml` is applied automatically: the API runs in the
  `dev` target (`dotnet watch`, repo mounted at `/workspace`) against a local
  **Postgres 17** with a healthcheck. The API waits for the database to be
  healthy.
- **`docker-compose.yml` is the base/CI image**: multi-stage `Dockerfile`
  (`base → build → publish → dev → final`), self-contained single-file publish,
  running as a non-root user (`APP_UID`).
- **Never bake configuration into an image.** Everything comes from environment
  variables (`ConnectionStrings__pg`, `Jwt__*`, `Cors__*`, `RateLimiting__*`).
  The image is identical across environments; only the env differs.
- If the `Dockerfile` gains a project, its `.csproj` must be added to the
  explicit `COPY` list so restore layer caching keeps working.
- **There is no CI/CD pipeline.** Images are built and pushed by hand
  (`docker compose build` / `push`) against the registry in `DOCKER_REGISTRY`,
  which is still a placeholder. Run the tests locally before pushing — nothing
  gates a merge automatically.

## 6. Security & runtime

- **JWT** HS256. `Jwt__SymmetricKey` (>= 32 bytes) is mandatory outside
  Development; the app refuses to start without it. Endpoints declare their
  allowed roles with `[AuthorizeRoles(...)]` (see section 9).
- **CORS** is fail-closed outside Development; origins come from
  `Cors__AllowedOrigins__0..N`.
- Global per-IP **rate limiting**, `SecurityHeadersMiddleware`, and a `/health`
  endpoint that checks Postgres (503 when it is down).
- **Secrets only via `.env` / environment variables** (see `.env.example`).
  Never in `appsettings*.json`, never committed. `.env` is gitignored.
- Structured logging with **Serilog**. Never log traceability payloads,
  credentials, or full documents — log identifiers.

## 7. Conventions

- **This file, and all rules/architecture docs, are written in English.**
  **New code is written in English too** — types, members, commands, queries and
  handlers (`User`, `CreateUserCommand`, `GetAuditEntriesQuery`). The legacy
  `Items` sample feature is still in Spanish; it is the template leftover and is
  not the naming reference.
- **Error codes are English and stable** (`User.EmailAlreadyExists`); **error
  messages are Spanish**, because they are shown to the end user. Same for
  catalog values that the business named in Spanish (`agente`, `en_proceso`).
- Nullable reference types and implicit usings are on, centralised in
  `Directory.Build.props`. Package versions are centralised in
  `Directory.Packages.props` (CPM) — never pin a version inside a `.csproj`.
- **Commits: short, English, Conventional Commits** — `feat(scope): ...`,
  `fix(scope): ...`, `chore: ...`. Commits are authored solely by the repository
  owner; no tool or assistant attribution, co-author trailers, or generated-by
  footers.
- Branch flow: `feature → main` through a PR. There is no automated check on a
  PR, so run the build and the unit tests yourself before merging.
- Tests: unit tests must run without a database
  (`--filter "FullyQualifiedName!~Integration"`); integration tests use
  Testcontainers and need Docker.

## 8. Domain model (implemented)

### Users, roles and statuses

- **`User`** (`user`) — `Name`, `Email` (unique, lowercased, used to log in),
  `PasswordHash`, `EmployeeNumber` (*legajo*, unique), `RoleId`, `IsActive`,
  `CreatedAt`, `LastLoginAt`. The plaintext password is **never** stored or
  logged, and `UserDto` never exposes the hash.
- **`Role`** (`role`) — fixed catalog seeded by migration, `ValueGeneratedNever`:
  `1 admin`, `2 supervisor`, `3 agente`. Use `RoleNames.*` / `RoleIds.*`, never
  string literals. Roles are read-only through the API.
- **`Status`** (`status`) — fixed catalog seeded by migration: `1 en_proceso`,
  `2 finalizado`, `3 cancelado`. `Code` is the stable identifier, `Name` the
  label to display. Entities with a lifecycle (document, proforma, lot) point at
  it with a `StatusId` **FK** — add the FK on the entity that owns the lifecycle,
  never duplicate the catalog.
- Catalog FKs use `DeleteBehavior.Restrict`: a referenced role or status can
  never be deleted out from under a row.

### Audit

- **`AuditEntry`** (`audit_entry`) — `UserId` (**the agent**, required FK to
  `user`, `Restrict`), `Action`, `EntityType`, `EntityId`, `Detail`, `IpAddress`,
  `OccurredAt` (UTC).
- Audit is **append-only**: never updated, never deleted, never cascaded.
- The IP is taken from the connection **on the server**, never from the request
  body — otherwise a client could forge it.
- Indexed for the query endpoint's filters: `OccurredAt`, `UserId`, `Action`,
  `(EntityType, EntityId)`.
- Known actions live in `AuditActions` (`login`, `login_failed`,
  `user_created`). Add a constant instead of a literal when you audit something
  new, and **never put sensitive data in `Detail`**.

## 9. Authentication & authorization

- **`POST /api/v1/auth/login`** (anonymous) takes email + password and returns
  the JWT, its expiry and the user's basic data.
- Passwords are hashed with **PBKDF2-SHA256**, 210k iterations, a random 16-byte
  salt per user, stored as `iterations.salt.hash`; verification is constant-time.
  No external hashing dependency.
- Login failures always return the **same** generic error
  (`Auth.InvalidCredentials`) — never reveal whether the email exists.
- The JWT carries `sub`, `jti`, `email`, name, **role** (`ClaimTypes.Role`) and
  `employee_number`. `Program.cs` resolves the signing key once and writes it
  back into configuration, so the API signs and validates with the *same* key.
- **Per-endpoint role arrays.** Use
  `[AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]` — the user needs **at
  least one** of the listed roles (OR). No roles = authentication only. This is
  the standard way to protect an endpoint; do not hand-roll role checks inside
  handlers.
- The **initial admin** is seeded at startup only when the user table is empty,
  from `Seed__AdminEmail` / `Seed__AdminPassword`. In Development it falls back
  to a documented dev account; outside Development it creates nothing and logs a
  warning.

### Account lifecycle — no self-registration

**There is no public sign-up, and there must never be one.** Accounts are
provisioned by an admin:

1. The initial admin is seeded at startup (above).
2. That admin creates every other user with `POST /api/v1/users`, choosing the
   role and the initial password.
3. The user changes that password with `POST /api/v1/auth/change-password`,
   which requires the current one. An admin who forgot nothing can still reset
   somebody else's password with `POST /api/v1/users/{id}/reset-password`.
4. Users are **never deleted** — audit rows reference them. Use
   `PATCH /api/v1/users/{id}/active` for the logical deactivation; an inactive
   user cannot authenticate. An admin cannot deactivate their own account.

Password rules live in one place, `PasswordPolicy` (minimum 8 characters) —
creation, reset and self-change all go through it.

## 10. Pagination (mandatory)

**Every list endpoint is paginated. No exceptions** — not even fixed catalogs
like roles and statuses, so the contract stays uniform.

- Query string: `?page=1&pageSize=20`. `page` is 1-based; `pageSize` defaults to
  20 and is **clamped to 100** by `PageRequest` — a client can never pull a whole
  table.
- Repositories return `PagedResult<T>` (`Items`, `Page`, `PageSize`,
  `TotalCount`, plus computed `TotalPages`, `HasPrevious`, `HasNext`) and
  handlers project it with `.Map(...)`, which preserves the metadata.
- **Paginate in the database** (`Skip`/`Take` over `IQueryable`) with a
  deterministic `OrderBy` plus a tie-breaker, so pages never overlap or skip
  rows. Count first, then fetch the page.

## 11. Metrics

Basic metrics are **generic and extensible** — nothing is hardcoded in the
handler or controller:

- `IMetricProvider` (Application port) exposes a `Key` (`users`, `audit`,
  `items`) and returns `MetricValue(Key, Label, Value, Group?)`. `Group`
  desaggregates one metric into several rows (users by role, audit by action).
- `GET /api/v1/metrics?source=users&source=audit&from=&to=` runs every registered
  provider (or only the requested ones), applies the date window and returns the
  values paginated.
- **To add metrics: write one more `IMetricProvider` in Infrastructure and
  register it in `DependencyInjection`.** Never touch the handler or the
  controller. `ItemMetricProvider` is the template to copy.

## 12. Endpoints

| Endpoint | Roles | Notes |
| --- | --- | --- |
| `POST /api/v1/auth/login` | anonymous | Issues the JWT |
| `GET /api/v1/auth/me` | authenticated | Current user, read from the DB |
| `POST /api/v1/auth/change-password` | authenticated | Own password; requires the current one |
| `GET /api/v1/users` | admin, supervisor | Paginated, filters: `search`, `roleId`, `isActive` |
| `GET /api/v1/users/{id}` | admin, supervisor | Detail |
| `POST /api/v1/users` | admin | Manual creation; 409 on duplicate email/employee number |
| `POST /api/v1/users/{id}/reset-password` | admin | Sets a new password without the old one |
| `PATCH /api/v1/users/{id}/active` | admin | Logical activate/deactivate |
| `GET /api/v1/roles` | authenticated | Paginated catalog |
| `GET /api/v1/statuses` | authenticated | Paginated catalog |
| `GET /api/v1/audit` | admin, supervisor | Paginated, filters: `userId`, `action`, `entityType`, `entityId`, `from`, `to` |
| `GET /api/v1/metrics` | admin, supervisor | Paginated, filters: `source`, `from`, `to` |
| `GET /api/v1/items` | authenticated-free (sample) | Paginated |
| `GET /health` | anonymous | Checks Postgres |

## 13. Configuration & environment

Configuration lives in **`.env`** (gitignored, read automatically by
`docker compose up`); `.env.example` documents every key. Never put secrets in
`appsettings*.json`.

| Variable | Meaning |
| --- | --- |
| `PG_CONN` / `ConnectionStrings__pg` | Postgres connection string |
| `Ef__AutoMigrate` | Apply pending EF migrations on startup |
| `Jwt__SymmetricKey` | HS256 key, >= 32 bytes, mandatory outside Development |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer/audience |
| `Jwt__ExpirationMinutes` | Access token lifetime (default 480) |
| `Seed__AdminEmail`, `Seed__AdminPassword`, `Seed__AdminEmployeeNumber` | Initial admin, only seeded when there are no users |
| `Cors__AllowedOrigins__0..N` | Allowed origins (fail-closed outside dev) |
| `RateLimiting__PermitLimit`, `RateLimiting__WindowSeconds` | Global per-IP rate limit |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Local Postgres container |
| `DOCKER_REGISTRY`, `API_IMAGE_TAG` | Deploy target (registry still a placeholder) |

Running the API outside Docker reads `appsettings.Development.json` instead of
`.env` — keep the local connection string in sync there.

## 14. Commands

```bash
dotnet build                                                              # build
dotnet test Papasur.Tests --filter "FullyQualifiedName!~Integration"      # unit tests
dotnet test Papasur.Tests --filter "FullyQualifiedName~Integration"       # integration (Docker)
dotnet run --project Papasur.Api                                          # local API, OpenAPI at /openapi/v1.json (dev only)
docker compose up                                                         # API hot-reload + Postgres 17
```
