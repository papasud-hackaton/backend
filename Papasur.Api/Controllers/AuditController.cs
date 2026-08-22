using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Audit.Queries.GetAuditEntries;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>Consulta de auditoría: paginada y con filtros combinables.</summary>
[ApiController]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    /// <summary>
    /// Auditoría filtrable por agente (userId), acción, entidad y rango de fechas (UTC).
    /// Ejemplo: /api/v1/audit?userId=...&amp;action=login&amp;from=2026-01-01&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet]
    [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
    public async Task<ActionResult<PagedResult<AuditEntryDto>>> List(
        [FromQuery] PageQuery page,
        [FromServices] IQueryHandler<GetAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var filter = new AuditFilter(userId, action, entityType, entityId, from, to);

        var result = await handler.Handle(
            new GetAuditEntriesQuery(page.ToPageRequest(), filter),
            cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }
}
