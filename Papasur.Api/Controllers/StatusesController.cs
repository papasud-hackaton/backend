using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Statuses.Queries.GetStatuses;

namespace Papasur.Api.Controllers;

/// <summary>Catálogo de estados (en proceso, finalizado, cancelado). Sólo lectura.</summary>
[ApiController]
[Route("api/v1/statuses")]
[Authorize]
public class StatusesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<StatusDto>>> List(
        [FromQuery] PageQuery page,
        [FromServices] IQueryHandler<GetStatusesQuery, PagedResult<StatusDto>> handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.Handle(new GetStatusesQuery(page.ToPageRequest()), cancellationToken));
    }
}
