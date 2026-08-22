# Vertical N°3 — Copiloto de documentación para exportación

Documento para el equipo. Explica **qué se construyó**, **cómo está armado**, **qué archivos se tocaron**, **cómo levantarlo** y **todos los endpoints** con ejemplos. Al final está el resultado de la prueba end-to-end.

---

## 1. Qué resuelve

Asistente para generar **facturas proforma y documentación de exportación** de papa semilla.

Idea central: el sistema conoce los **requisitos documentales** (plantillas) y los cruza con la **trazabilidad de un lote** para **pre-completar por inferencia** lo que ya se sabe. Lo que no se puede inferir queda para carga **manual o por dictado**. Nada se da por válido hasta que **una persona confirma** (revisión humana explícita) y todo queda **auditado**.

Principios (según `ai.md`):
- El **Lote** es el centro del dominio.
- Los requisitos documentales son **datos** (tabla `plantilla_documento` + `campo_plantilla`), no código.
- La inferencia es **asistiva**: sugiere, no decide.

---

## 2. Arquitectura (dónde vive cada cosa)

Se respetó la Clean Architecture y el CQRS hand-rolled ya existentes en el repo:

```
Papasur.Domain          → entidades puras (sin dependencias)
Papasur.Application     → casos de uso: commands/queries + handlers + PORTS (interfaces) + DTOs + motor de inferencia
Papasur.Infrastructure  → EF Core: DbContext, migración, repos (adapters de los ports), seeder
Papasur.Api             → controllers REST (resuelven handlers por DI)
```

Regla de dependencias: `Api → Application → Domain` y `Infrastructure → Application/Domain`. Los controllers **nunca** exponen entidades de dominio, siempre DTOs.

---

## 3. Modelo de datos (nuevas tablas)

Todas en `snake_case`, PK `Guid`. Migración: `20260822164148_TrazabilidadYDocumentos`.

### Trazabilidad
| Tabla | Descripción | Campos clave |
|---|---|---|
| `variedad` | Variedad de papa semilla | `nombre` (único) |
| `campo` | Finca/lote geográfico de origen | `nombre`, `establecimiento`, `pivote`, `cuadrante` |
| `transportista` | Transportista del despacho | `nombre` (único) |
| `cliente` | Cliente/importador comercial | `nombre`, `pais` |
| `lote` | **Unidad central** de trazabilidad | `codigo`, `variedad_id`→variedad, `campo_id`→campo, `categoria`, `superficie_ha` |
| `movimiento` | Despacho/movimiento de un lote | `lote_id`, `tipo`, `numero_remito`, `fecha`, `kilogramos`, `bolsas`, `kg_promedio`, `presentacion`, `categoria`, `calibre`, `transportista_id`, `cliente_id`, `comisionista`, `destino`, `dtv`, `observaciones` |

### Documentos
| Tabla | Descripción | Campos clave |
|---|---|---|
| `plantilla_documento` | Requisito documental (proforma, fito, etc.) | `nombre`, `tipo`, `organismo`, `pais_destino`, `version`, `activa` |
| `campo_plantilla` | Cada campo que exige una plantilla | `plantilla_documento_id`, `clave`, `etiqueta`, `tipo_dato`, `obligatorio`, **`regla_mapeo`**, `orden` |
| `documento_exportacion` | Documento generado para un lote | `lote_id`, `movimiento_id`, `plantilla_documento_id`, `version_plantilla`, `status_id`, `created_by_user_id`, `created_at`, `confirmed_at` |
| `valor_campo` | Valor de cada campo del documento | `documento_exportacion_id`, `campo_plantilla_id`, `valor`, **`origen`** (`Inferido`/`Manual`/`Dictado`), `confirmado`, `inferido_desde` |

`regla_mapeo` es la pieza que conecta un requisito con la trazabilidad (ej. `movimiento.dtv`). `origen` + `inferido_desde` dan la **traza auditable** de cada dato.

---

## 4. Motor de inferencia

`MotorInferenciaReglas` (en `Application/Documentos/Inference`) implementa el puerto `IMotorInferencia`. Es **determinístico**: por cada `campo_plantilla` toma su `regla_mapeo` y resuelve el valor contra el lote y el movimiento elegido.

Está detrás de un puerto a propósito: una futura versión con **LLM** entra en `Infrastructure` implementando `IMotorInferencia`, sin tocar handlers ni controllers.

### Reglas de mapeo soportadas
| `regla_mapeo` | Fuente |
|---|---|
| `lote.codigo` | Lote.Codigo |
| `lote.variedad` | Lote.Variedad.Nombre |
| `lote.campo` | Lote.Campo.Nombre |
| `lote.establecimiento` | Lote.Campo.Establecimiento |
| `lote.categoria` | Lote.Categoria |
| `lote.superficie_ha` | Lote.SuperficieHa |
| `movimiento.numero_remito` | Movimiento.NumeroRemito |
| `movimiento.fecha` | Movimiento.Fecha (`yyyy-MM-dd`) |
| `movimiento.kilogramos` | Movimiento.Kilogramos |
| `movimiento.bolsas` | Movimiento.Bolsas |
| `movimiento.kg_promedio` | Movimiento.KgPromedio |
| `movimiento.presentacion` | Movimiento.Presentacion |
| `movimiento.categoria` | Movimiento.Categoria |
| `movimiento.calibre` | Movimiento.Calibre |
| `movimiento.transportista` | Movimiento.Transportista.Nombre |
| `movimiento.cliente` | Movimiento.Cliente.Nombre |
| `movimiento.pais` | Movimiento.Cliente.Pais |
| `movimiento.comisionista` | Movimiento.Comisionista |
| `movimiento.destino` | Movimiento.Destino |
| `movimiento.dtv` | Movimiento.Dtv |

Si un campo **no tiene** `regla_mapeo` o el dato no existe → queda vacío con `origen = Manual` (pendiente de carga humana/dictado).

---

## 5. Flujo funcional

1. **Seleccionar lote** → `GET /lotes` y `GET /lotes/{id}` (trae movimientos).
2. **Elegir plantilla** → `GET /documentos/plantillas`.
3. **Generar borrador** → `POST /documentos/generar`. El motor pre-completa; se persiste un `documento_exportacion` en estado **En proceso** con un `valor_campo` por cada campo.
4. **Revisar** → `GET /documentos/{id}`: cada campo con su `valor`, `origen` y si es `obligatorio`.
5. **Confirmar** → `POST /documentos/{id}/confirmar`: se aplican ediciones humanas (pasan a `Manual`/`Dictado`), se **validan obligatorios** y el documento pasa a **Finalizado** con `confirmed_at`.

Cada generación y confirmación se registra en auditoría (`document_generated`, `document_confirmed`).

---

## 6. Endpoints

Base local: `http://localhost:5080`. Todos menos login requieren `Authorization: Bearer <token>`.

### Auth
```
POST /api/v1/auth/login
Body:  { "email": "admin@papasud.com", "password": "****" }
200:   { "accessToken": "...", "expiresAt": "...", "email": "...", ... }
```

### Lotes (trazabilidad, solo lectura)
```
GET /api/v1/lotes?search=&variedadId=&page=1&pageSize=20
200: PagedResult<LoteDto>
     { items: [{ id, codigo, variedadId, variedad, campoId, campo,
                 categoria, superficieHa, cantidadMovimientos, createdAt }],
       page, pageSize, totalCount }

GET /api/v1/lotes/{id}
200: LoteDetalleDto
     { id, codigo, variedad, campo, establecimiento, categoria, superficieHa,
       createdAt, movimientos: [{ id, tipo, numeroRemito, fecha, kilogramos,
       bolsas, kgPromedio, presentacion, categoria, calibre, transportista,
       cliente, pais, comisionista, destino, dtv, observaciones }] }
404: Lote.NotFound
```

### Documentos (copiloto)
```
GET /api/v1/documentos/plantillas?soloActivas=true&page=1&pageSize=20
200: PagedResult<PlantillaDto>
     { items: [{ id, nombre, tipo, organismo, paisDestino, version, activa, cantidadCampos }], ... }

POST /api/v1/documentos/generar
Body:  { "loteId": "guid", "plantillaDocumentoId": "guid", "movimientoId": "guid|null" }
201:   "guid-del-documento"   (+ header Location)
400:   Plantilla.Inactiva
404:   Lote.NotFound | Plantilla.NotFound | Movimiento.NotFound

GET /api/v1/documentos/{id}
200: DocumentoExportacionDto
     { id, loteId, loteCodigo, variedad, movimientoId, dtv,
       plantillaDocumentoId, plantilla, tipo, versionPlantilla,
       statusId, status, createdAt, confirmedAt,
       campos: [{ campoPlantillaId, clave, etiqueta, tipoDato,
                  obligatorio, orden, valor, origen, confirmado, inferidoDesde }] }
404: Documento.NotFound

POST /api/v1/documentos/{id}/confirmar
Body:  { "campos": [ { "campoPlantillaId": "guid", "valor": "texto", "porDictado": false } ] }
204:   (sin contenido) — documento Finalizado
400:   Documento.CamposObligatorios (faltan requeridos) | Documento.CampoInvalido
404:   Documento.NotFound
409:   Documento.YaConfirmado
```

Los errores siguen el formato `ProblemDetails` (`title` = código, `detail` = mensaje).

---

## 7. Datos de demo (seed)

`TrazabilidadSeeder` siembra automáticamente en **Development** si no hay lotes (idempotente). Datos reales extraídos de la planilla de movimientos:

- **Variedades**: agata, spunta, king russet, asterix.
- **Campo**: Marisol (establecimiento Santa Ana, pivote B).
- **5 lotes** (`224`, `241`, `300`, `821`, `910`) con **8 movimientos** (remitos, DTV, calibre exportación, transportistas y clientes reales).
- **Plantilla** "Proforma de exportación de semilla" (SENASA, Brasil) con **17 campos**: 13 con `regla_mapeo` (inferibles) + 4 manuales (`exportador`, `precio_unitario_usd`, `incoterm`, `observaciones`).

> Nota: se optó por seed de código (no un importador de `.xls` en runtime) para no agregar dependencias NuGet ni depender de un archivo externo. Si se necesita cargar el Excel real vía endpoint de upload, es un paso siguiente acotado.

---

## 8. Cómo levantar la API en local

Los secretos viven en `backend/.env` (gitignoreado). `dotnet run` **no** lee `.env` solo, así que se cargan a variables de entorno y se mapea `PG_CONN → ConnectionStrings__pg`.

```powershell
cd backend

# Cargar variables desde .env a la sesión
Get-Content .env | ForEach-Object {
  $line = $_.Trim()
  if ($line -and -not $line.StartsWith('#')) {
    $idx = $line.IndexOf('=')
    if ($idx -gt 0) {
      [System.Environment]::SetEnvironmentVariable($line.Substring(0,$idx).Trim(), $line.Substring($idx+1).Trim())
    }
  }
}
$env:ConnectionStrings__pg = $env:PG_CONN
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5080'

dotnet run --project Papasur.Api
```

Alternativa con Docker (levanta también Postgres 17 local): `docker compose up` (usa `docker-compose.override.yml`).

Al arrancar: aplica migraciones pendientes (`Ef__AutoMigrate=true`), crea el admin del seed si no existe y siembra la trazabilidad de demo. Escucha en `http://localhost:5080`. OpenAPI en `/openapi/v1.json`. CORS permite cualquier `localhost` en Development.

---

## 9. Archivos creados/modificados

### Domain
- **Nuevos** `Trazabilidad/`: `Variedad.cs`, `Campo.cs`, `Transportista.cs`, `Cliente.cs`, `Lote.cs`, `Movimiento.cs` (+ `TiposMovimiento`).
- **Nuevos** `Documentos/`: `PlantillaDocumento.cs` (+ `TiposDocumento`), `CampoPlantilla.cs` (+ `TiposDato`), `DocumentoExportacion.cs`, `ValorCampo.cs` (+ enum `OrigenValor`).
- **Modificado** `Audit/AuditEntry.cs`: constantes `AuditActions.DocumentGenerated` y `DocumentConfirmed`.

### Application
- **Nuevo** `Trazabilidad/Ports/ILoteRepository.cs`.
- **Nuevos** `Trazabilidad/Queries/ObtenerLotes/`: `LoteDto`, `ObtenerLotesQuery`, `ObtenerLotesQueryHandler`.
- **Nuevos** `Trazabilidad/Queries/ObtenerLotePorId/`: `MovimientoDto`, `LoteDetalleDto`, `ObtenerLotePorIdQuery`, handler.
- **Nuevos** `Documentos/Ports/`: `IPlantillaRepository.cs`, `IDocumentoRepository.cs`.
- **Nuevos** `Documentos/Inference/`: `IMotorInferencia.cs`, `MotorInferenciaReglas.cs`.
- **Nuevos** `Documentos/Queries/ObtenerDocumento/`: `ValorCampoDto`, `DocumentoExportacionDto`, query, handler.
- **Nuevos** `Documentos/Queries/ObtenerPlantillas/`: `PlantillaDto`, query, handler.
- **Nuevos** `Documentos/Commands/GenerarBorrador/`: `GenerarBorradorCommand`, handler.
- **Nuevos** `Documentos/Commands/ConfirmarDocumento/`: `ConfirmarDocumentoCommand` (+ `CampoEdicion`), handler.
- **Modificado** `DependencyInjection.cs`: registro de handlers + `IMotorInferencia`.

### Infrastructure
- **Modificado** `Persistence/AppDbContext.cs`: `DbSet`s + `OnModelCreating` (tablas snake_case, FKs, índices, enum a string).
- **Nuevos** `Persistence/Migrations/20260822164148_TrazabilidadYDocumentos.cs` (+ `.Designer.cs`).
- **Nuevo** `Trazabilidad/EfLoteRepository.cs`.
- **Nuevos** `Documentos/EfPlantillaRepository.cs`, `Documentos/EfDocumentoRepository.cs`.
- **Nuevo** `Persistence/TrazabilidadSeeder.cs`.
- **Modificado** `DependencyInjection.cs`: registro de repos y seeder.

### Api
- **Nuevo** `Controllers/LotesController.cs`.
- **Nuevo** `Controllers/DocumentosController.cs` (+ `GenerarBorradorRequest`, `ConfirmarDocumentoRequest`).
- **Modificado** `Program.cs`: invocación del `TrazabilidadSeeder` en Development.

---

## 10. Resultado de la prueba end-to-end

Ejecutada contra la base real (Render) con el seed de demo. Lote `224`, movimiento de exportación (remito `805`, DTV `13250335-4`, cliente Dospanca/Brasil), plantilla proforma.

**Borrador generado (estado `En proceso`) — 13/17 campos inferidos automáticamente:**

| # | clave | valor | origen | obligatorio |
|---|---|---|---|---|
| 0 | lote | 224 | Inferido | ✔ |
| 1 | variedad | agata | Inferido | ✔ |
| 2 | campo_origen | Marisol | Inferido | |
| 3 | categoria | exportacion | Inferido | |
| 4 | remito | 805 | Inferido | |
| 5 | fecha | 2026-03-07 | Inferido | |
| 6 | peso_neto_kg | 29120 | Inferido | ✔ |
| 7 | bolsas | 568 | Inferido | |
| 8 | kg_por_bolsa | 51.26 | Inferido | |
| 9 | transportista | Alvaro Arenas | Inferido | |
| 10 | cliente | Dospanca | Inferido | ✔ |
| 11 | pais_destino | Brasil | Inferido | ✔ |
| 12 | dtv | 13250335-4 | Inferido | ✔ |
| 13 | exportador | *(vacío)* | Manual | ✔ |
| 14 | precio_unitario_usd | *(vacío)* | Manual | ✔ |
| 15 | incoterm | *(vacío)* | Manual | |
| 16 | observaciones | *(vacío)* | Manual | |

**Confirmación:** se completaron los 2 obligatorios vacíos (`exportador`, `precio_unitario_usd`). La validación pasó y el documento quedó en **`Finalizado`** con `confirmedAt` seteado y todos los campos marcados `confirmado = true`.

El flujo completo (login → listar → generar → revisar → confirmar) funciona end-to-end.
