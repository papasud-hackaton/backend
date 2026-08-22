using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
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
        [FromQuery] PageQuery page,
        [FromServices] IQueryHandler<GetRolesQuery, PagedResult<RoleDto>> handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.Handle(new GetRolesQuery(page.ToPageRequest()), cancellationToken));
    }
}
