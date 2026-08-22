using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Metrics.Ports;
using Papasur.Application.Metrics.Queries.GetMetrics;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>
/// Métricas básicas del sistema, paginadas y acotables por rango de fechas.
/// Es genérico: cada área aporta las suyas implementando IMetricProvider.
/// </summary>
[ApiController]
[Route("api/v1/metrics")]
public class MetricsController : ControllerBase
{
    /// <summary>
    /// Ejemplo: /api/v1/metrics?source=users&amp;source=audit&amp;from=2026-01-01&amp;pageSize=50
    /// </summary>
    [HttpGet]
    [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
    public async Task<ActionResult<PagedResult<MetricDto>>> List(
        [FromServices] IQueryHandler<GetMetricsQuery, Result<PagedResult<MetricDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery(Name = "source")] string[]? sources = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await handler.Handle(
            new GetMetricsQuery(new PageRequest(page, pageSize), new MetricFilter(from, to), sources),
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "Metrics.SourceNotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
        }

        return Ok(result.Value);
    }
}
