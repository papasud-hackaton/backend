using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Audit.Queries.GetAuditEntries;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>
/// Consulta de auditoría (contrato §6). Exige audit.viewAll → supervisor y admin.
/// No existe alta desde el cliente: las entradas las escribe el backend.
/// </summary>
[Route("api/v1/audit-logs")]
[AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
public class AuditController : ApiControllerBase
{
    /// <summary>
    /// action y role son REPETIBLES: ?action=user.login&amp;action=form.created.
    /// from/to acotan por fecha (UTC).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> List(
        [FromServices] IQueryHandler<GetAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] Guid? actorId = null,
        [FromQuery(Name = "action")] string[]? actions = null,
        [FromQuery(Name = "role")] string[]? roles = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await handler.Handle(
            new GetAuditEntriesQuery(new PageRequest(page, pageSize), BuildFilter()),
            cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : Ok(result.Value);

        AuditFilter BuildFilter() => new(
            actorId,
            actions is { Length: > 0 } ? actions : null,
            roles is { Length: > 0 } ? roles : null,
            entityType,
            entityId,
            search,
            from,
            to);
    }

    /// <summary>Misma consulta sin paginar, en CSV, para descargar.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromServices] IQueryHandler<ExportAuditEntriesQuery, Result<string>> handler,
        CancellationToken cancellationToken,
        [FromQuery] Guid? actorId = null,
        [FromQuery(Name = "action")] string[]? actions = null,
        [FromQuery(Name = "role")] string[]? roles = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var filter = new AuditFilter(
            actorId,
            actions is { Length: > 0 } ? actions : null,
            roles is { Length: > 0 } ? roles : null,
            entityType,
            entityId,
            search,
            from,
            to);

        var result = await handler.Handle(new ExportAuditEntriesQuery(filter), cancellationToken);

        if (result.IsFailure)
        {
            return Fail(StatusCodes.Status400BadRequest, result.Error);
        }

        var nombre = $"auditoria-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(result.Value), "text/csv", nombre);
    }
}
