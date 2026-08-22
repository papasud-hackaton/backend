using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Commands.ResetUserPassword;
using Papasur.Application.Users.Commands.SetUserActive;
using Papasur.Application.Users.Queries.GetUserById;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>
/// Administración de usuarios. El alta es SIEMPRE manual y sólo la hace un admin:
/// no existe registro público (por diseño).
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    /// <summary>Listado paginado de usuarios, con filtros por texto, rol y estado.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromServices] IQueryHandler<GetUsersQuery, PagedResult<UserDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await handler.Handle(
            new GetUsersQuery(new PageRequest(page, pageSize), search, roleId, isActive),
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

    /// <summary>Detalle de un usuario.</summary>
    [HttpGet("{id:guid}")]
    [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)]
    public async Task<ActionResult<UserDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetUserByIdQuery, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetUserByIdQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>Reseteo de contraseña de otro usuario (sólo admin, sin pedir la anterior).</summary>
    [HttpPost("{id:guid}/reset-password")]
    [AuthorizeRoles(RoleNames.Admin)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        [FromServices] ICommandHandler<ResetUserPasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ResetUserPasswordCommand(id, request.NewPassword)
            {
                PerformedByUserId = GetCurrentUserId(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        return MapEmptyResult(result);
    }

    /// <summary>
    /// Alta/baja lógica del usuario (sólo admin). Los usuarios no se borran nunca:
    /// la auditoría los referencia.
    /// </summary>
    [HttpPatch("{id:guid}/active")]
    [AuthorizeRoles(RoleNames.Admin)]
    public async Task<IActionResult> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        [FromServices] ICommandHandler<SetUserActiveCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SetUserActiveCommand(id, request.IsActive)
            {
                PerformedByUserId = GetCurrentUserId(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        return MapEmptyResult(result);
    }

    private IActionResult MapEmptyResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        var status = result.Error.Code == "User.NotFound"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
    }

    private Guid? GetCurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

/// <summary>Body del reseteo de contraseña.</summary>
public sealed record ResetPasswordRequest(string NewPassword);

/// <summary>Body del alta/baja lógica.</summary>
public sealed record SetActiveRequest(bool IsActive);
