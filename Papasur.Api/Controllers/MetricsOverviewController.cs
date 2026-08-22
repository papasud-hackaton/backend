using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Metrics.Queries.GetOverview;

namespace Papasur.Api.Controllers;

/// <summary>
/// Tablero (contrato §7). scope=me devuelve lo propio; scope=team, la foto del equipo.
/// Un agente que pide team recibe lo suyo, NO un 403: degradar es mejor que romper la pantalla.
/// </summary>
[Route("api/v1/metrics/overview")]
public class MetricsOverviewController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] IQueryHandler<GetOverviewQuery, Result<MetricsOverviewResult>> handler,
        CancellationToken cancellationToken,
        [FromQuery] string? scope = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new GetOverviewQuery(scope, from, to, actor), cancellationToken);

        return Ok((object?)result.Value.Team ?? result.Value.Agent);
    }
}
