using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Roles.Queries.GetRoles;

namespace Papasur.Api.Controllers;

/// <summary>Catálogo de roles (admin, supervisor, agente). Sólo lectura.</summary>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RoleDto>>> List(
        [FromServices] IQueryHandler<GetRolesQuery, PagedResult<RoleDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        return Ok(await handler.Handle(new GetRolesQuery(new PageRequest(page, pageSize)), cancellationToken));
    }
}
