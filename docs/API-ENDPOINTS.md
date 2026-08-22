# API — Endpoints implementados

Estado real del backend, extraído del código (no del contrato). Última
actualización: commit `ebdd833`.

**Base URL:** `https://<host>/api/v1` — el front tiene que apuntar ahí
`VITE_API_URL`, incluyendo el `/api/v1`.

---

## 0. Reglas generales

### Autenticación
`Authorization: Bearer <token>` salvo en `/auth/login`,
`/auth/forgot-password` y `/auth/reset-password`.

El JWT es HS256 y lleva `sub`, `email`, nombre, **rol** y `employee_id`.
Vence a las 8 h por defecto (`Jwt__ExpirationMinutes`).

### Forma del error
Toda respuesta que no sea 2xx:

```json
{ "message": "Texto para mostrarle al usuario, en español.", "code": "codigo_para_la_maquina" }
```

### Paginación
Todo listado paginado acepta `?page=1&pageSize=20` y devuelve:

```json
{ "items": [], "page": 1, "pageSize": 20, "total": 150,
  "totalPages": 8, "hasPrevious": false, "hasNext": true }
```

`pageSize` está topeado en **100**; `page` es 1-based.

### Roles
`admin` · `supervisor` · `agent`

---

## 1. Autenticación — 6/6

| Método | Ruta | Acceso |
| --- | --- | --- |
| POST | `/auth/login` | anónimo |
| GET | `/auth/me` | autenticado |
| POST | `/auth/logout` | autenticado |
| POST | `/auth/forgot-password` | anónimo |
| POST | `/auth/reset-password` | anónimo |
| PATCH | `/auth/password` | autenticado |

### `POST /auth/login`
```json
→ { "email": "admin@papasud.com", "password": "..." }
← 200 { "user": User, "token": "eyJ...", "expiresAt": "2026-08-23T00:00:00Z" }
← 401 { "message": "El correo o la contraseña no son correctos.", "code": "auth_invalid_credentials" }
← 403 { "message": "Tu cuenta está desactivada. Contactá a un administrador.", "code": "auth_disabled" }
```
El **401 es idéntico** para usuario inexistente, contraseña incorrecta y usuario
invitado sin contraseña definida: no se filtra qué cuentas existen. Actualiza
`lastLoginAt` y registra `user.login`.

### `GET /auth/me` → `200 User`
Lee de la base, no de los claims del token.

### `POST /auth/logout` → `204`
El JWT es stateless: no invalida nada, deja el rastro `user.logout`.

### `POST /auth/forgot-password`
```json
→ { "email": "..." }
← 204   ← siempre, exista o no la cuenta
```
Genera un token de un solo uso, vigente **2 horas**, guardado hasheado.
Pedir uno nuevo invalida el anterior. Registra `user.password_reset_requested`.

### `POST /auth/reset-password`
```json
→ { "token": "...", "password": "..." }
← 204
← 410 { "message": "El enlace venció. Pedí uno nuevo.", "code": "auth_token_expired" }
← 400 si la contraseña no cumple la política (mínimo 8 caracteres)
```
Es también el camino por el que un usuario **invitado se activa**.

### `PATCH /auth/password`
```json
→ { "currentPassword": "...", "newPassword": "..." }
← 204
← 400 { "message": "La contraseña actual no es correcta.", "code": "auth_current_password_invalid" }
```
Invalida los enlaces de recuperación pendientes. Registra `user.password_changed`.

---

## 2. Usuarios — 4/4 · solo `admin`

| Método | Ruta |
| --- | --- |
| GET | `/users?page=&pageSize=&search=&role=&status=` |
| GET | `/users/{id}` |
| POST | `/users` |
| PATCH | `/users/{id}` |
| POST | `/users/{id}/deactivate` |

`search` busca en nombre, apellido, correo y legajo.

### `POST /users`
```json
→ { "firstName", "lastName", "email", "employeeId", "role", "phone?" }
← 201 User
← 400 code: user_email_already_exists | user_employee_id_already_exists |
             user_email_invalid | user_role_not_found | ...
```
**No lleva contraseña.** El usuario nace con `status: "invited"` y el backend
emite una invitación (vigente 7 días). Registra `user.created`.

### `PATCH /users/{id}`
```json
→ { "firstName?", "lastName?", "phone?", "role?" }
← 200 User
```
Correo y legajo no se editan: son identidad. Un cambio de rol registra
`user.role_changed` con el valor anterior y el nuevo.

### `POST /users/{id}/deactivate` → `200 User`
Baja **lógica**: no hay DELETE, los formularios históricos conservan su autor.
Un admin no puede desactivarse a sí mismo.

### `User`
```json
{ "id", "firstName", "lastName", "email", "employeeId",
  "role": "admin|supervisor|agent",
  "status": "invited|active|inactive",
  "phone": null, "createdAt", "lastLoginAt": null }
```

---

## 3. Catálogo — 5/5

| Método | Ruta |
| --- | --- |
| GET | `/locations` |
| GET | `/customers?search=` |
| POST | `/customers` |
| GET | `/lots?page=&pageSize=&search=&locationId=&category=&status=` |
| GET | `/lots/{id}` |

`GET /locations` y `GET /customers` devuelven arrays sin paginar.
`GET /lots` sí pagina; el front pide `pageSize=100` y cachea el índice.

En los lotes, **`availableKg` se deriva de los movimientos reales**, no de un
campo guardado: de ahí salen los warnings de stock en vivo.

### `POST /customers`
```json
→ { "name", "taxId", "countryCode", "address", "city", "contactName?", "contactEmail?" }
← 201 Customer
```
Alta rápida desde el paso 1 del wizard, sin salir del formulario.

---

## 4. Requisitos documentales — 1/2

| Método | Ruta |
| --- | --- |
| GET | `/document-types` |

Devuelve las definiciones con sus campos y el `path` de autocompletado. El
motor de requisitos del front es data-driven: cambiar plantillas es cambiar
esta respuesta, no código.

**Pendiente:** `PUT /document-types/{code}`.

---

## 5. Formularios de exportación — 6/7

| Método | Ruta |
| --- | --- |
| GET | `/forms?page=&pageSize=&status=&status=&createdBy=&search=` |
| GET | `/forms/{id}` |
| POST | `/forms` |
| PATCH | `/forms/{id}` |
| POST | `/forms/{id}/transition` |
| POST | `/forms/{id}/documents` |

`status` es **repetible**. `createdBy=me` filtra por el usuario del token.
Un `agent` recibe **solo los propios**, filtrado en el servidor, sin importar
qué mande.

### `POST /forms` → `201 ExportForm`
El servidor asigna `id`, `code` (`PF-2026-0061`, correlativo por año),
`status: "draft"`, `version: 1` y `createdBy`. Registra `form.created`.

### `PATCH /forms/{id}` — bloqueo optimista
Header **`If-Match: <version>`**.

```
← 200 ExportForm      versión coincide; version sube en 1
← 409 conflicto de versión
← 403 si el formulario no es editable
```

```json
409 {
  "message": "Alguien más editó este formulario mientras lo tenías abierto.",
  "code": "version_conflict",
  "currentVersion": 7,
  "current": { ...el ExportForm completo tal como está guardado... }
}
```

Solo se aceptan estos campos; **todo lo demás se ignora** aunque venga:

```
customerId · destinationCountryCode · portOfLoading · portOfDischarge
incoterm · currency · paymentTerms · validUntil · notes
items · requirementValues
```

`totals` y `warnings` se **recalculan** en el servidor a partir de `items`.
`status` solo cambia por transición. `id`, `code`, `version`, `createdBy` y
`createdAt` los fija el servidor.

Solo `draft` y `changes_requested` son editables, y solo por el dueño o un admin.

### `POST /forms/{id}/transition` — máquina de estados
```json
→ { "action": "submit|request_changes|approve|issue|cancel|reopen",
    "reviewNotes": "obligatorio en request_changes",
    "reason": "obligatorio en cancel y reopen" }
← 200 ExportForm
← 400 si falta el texto obligatorio
← 403 con el motivo exacto en message
```

| Acción | De → A | Quién | Precondición |
| --- | --- | --- | --- |
| `submit` | draft, changes_requested → submitted | dueño, supervisor, admin | ≥1 línea y sin warnings `blocking` |
| `request_changes` | submitted → changes_requested | supervisor, admin | `reviewNotes` |
| `approve` | submitted → approved | supervisor, admin | — |
| `issue` | approved → issued | supervisor, admin | documentos generados |
| `cancel` | todos salvo issued → cancelled | dueño solo en draft; supervisor, admin | `reason` |
| `reopen` | approved, issued → draft | **solo admin** | `reason` |

El `403` trae el motivo real, que el front muestra tal cual: *"Resolvé las
advertencias bloqueantes."*, *"Generá la documentación antes de emitir."*,
*"El formulario no es tuyo."*

Cada transición registra su acción de auditoría con `{ field: "status", from, to }`.

### `POST /forms/{id}/documents` → `200 GeneratedDocument[]`
Genera los 6 documentos del envío. **Hoy solo los marca como generados**: falta
producir los archivos y devolver `fileUrl`.

### Warnings (recalculados siempre en el servidor)

| Código | Severidad | Cuándo |
| --- | --- | --- |
| `lot_quarantined` | **blocking** | el lote está en cuarentena |
| `insufficient_stock` | **blocking** | se piden más kilos de los disponibles |
| `stale_inventory` | warning | último inventario hace más de 90 días |
| `germination_below_threshold` | warning | poder germinativo por debajo de 85 % |
| `missing_traceability_field` | warning | el lote no tiene registro INASE |
| `mixed_categories` | info | el envío mezcla categorías de semilla |

Solo los `blocking` impiden enviar a revisión.

**Pendiente:** `GET /forms/{id}/preview?type=` (PDF armado en el servidor).

---

## 6. Auditoría — 2/2 · `supervisor` y `admin`

| Método | Ruta |
| --- | --- |
| GET | `/audit-logs?page=&pageSize=&actorId=&action=&action=&role=&entityType=&entityId=&search=&from=&to=` |
| GET | `/audit-logs/export` — mismos filtros, sin paginar |

`action` y `role` son **repetibles**. La exportación devuelve `text/csv` con
`Content-Disposition: attachment; filename="auditoria-AAAA-MM-DD.csv"`.

La auditoría es **inmutable y desnormalizada**: cada entrada guarda `actorName`
y `actorRole` como eran en ese momento, no una FK. No existe alta desde el
cliente.

**Acciones:** `user.login` · `user.logout` · `user.password_reset_requested` ·
`user.password_changed` · `user.profile_updated` · `user.created` ·
`user.updated` · `user.deactivated` · `user.role_changed` · `form.created` ·
`form.updated` · `form.submitted` · `form.approved` · `form.changes_requested` ·
`form.issued` · `form.cancelled` · `form.reopened` · `document.generated` ·
`document.confirmed` · `document.downloaded` · `settings.updated`

---

## 7. Métricas — 1/1

| Método | Ruta |
| --- | --- |
| GET | `/metrics/overview?scope=me\|team&from=&to=` |

- `scope=me` → `draftCount`, `submittedCount`, `approvedCount`, `formsThisMonth`
- `scope=team` → lo anterior más `formsByStatus`, `exportedVolumeKg`,
  `avgReviewTimeHours`, `changesRequestedRate`, `stockWarningsCount`,
  `topDestinations`, `topVarieties`

Un `agent` que pida `scope=team` recibe sus propios datos, **no un 403**.

---

## 8. Copiloto de documentación (vertical 3)

Endpoints propios del copiloto sobre trazabilidad de lotes, previos al
contrato del front. Conviven con `/forms`.

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/lotes?page=&pageSize=&search=&variedadId=` | autenticado |
| GET | `/lotes/{id}` | autenticado |
| GET | `/documentos/plantillas?soloActivas=` | autenticado |
| POST | `/documentos/generar` | autenticado |
| GET | `/documentos/{id}` | autenticado |
| POST | `/documentos/{id}/confirmar` | autenticado |

El motor de inferencia detrás de `/documentos/generar` es el candidato natural
para alimentar `POST /ai/prefill` cuando se implemente.

---

## 9. Operación

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/health` | anónimo — verifica Postgres, 503 si está caído |
| GET | `/roles` | autenticado — catálogo |
| GET | `/statuses` | autenticado — catálogo |
| GET | `/metrics` | admin, supervisor — métricas genéricas por proveedor |
| GET/POST | `/items` | feature de ejemplo del template |

OpenAPI en `/openapi/v1.json`, solo en Development.

---

## 10. Pendientes — 4 de 31

| Prioridad | Endpoint |
| --- | --- |
| Media | `PUT /document-types/{code}` |
| Media | `PATCH /users/me/preferences` |
| Baja | `GET /forms/{id}/preview` |
| Baja | `POST /ai/parse-dictation` · `POST /ai/prefill` |

---

## 11. Autorización

Política **fail-closed**: todo endpoint exige token salvo los marcados
`[AllowAnonymous]` — `/auth/login`, `/auth/forgot-password`,
`/auth/reset-password` y `/health`. Un controller que se olvide de
`[Authorize]` queda cerrado, no abierto.

Verificado: los seis endpoints que antes estaban abiertos (`/locations`,
`/customers`, `/lots`, `/document-types`, `/metrics/overview`, `/items`)
devuelven **401** sin token y 200 con token.
