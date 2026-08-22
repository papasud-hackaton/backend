using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    /// <summary>Listado paginado de usuarios, con filtros por texto, rol y estado.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromQuery] PageQuery page,
        [FromServices] IQueryHandler<GetUsersQuery, PagedResult<UserDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await handler.Handle(
            new GetUsersQuery(page.ToPageRequest(), search, roleId, isActive),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Alta de usuario (sólo admin). La contraseña se guarda hasheada.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleNames.Admin)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateUserCommand command,
        [FromServices] ICommandHandler<CreateUserCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var enriched = command with
        {
            PerformedByUserId = GetCurrentUserId(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };

        var result = await handler.Handle(enriched, cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code.EndsWith("AlreadyExists", StringComparison.Ordinal)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
        }

        return CreatedAtAction(nameof(List), new { id = result.Value }, result.Value);
    }

    private Guid? GetCurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
