using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Application.Auth.Commands.Login;
using Papasur.Application.Auth.Queries.GetCurrentUser;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Api.Controllers;

/// <summary>
/// Autenticación: emisión del JWT y operaciones sobre la cuenta propia.
/// NO hay registro público: los usuarios los crea un admin en POST /api/v1/users.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Login por correo + contraseña. Devuelve el access token y su vencimiento.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        [FromServices] ICommandHandler<LoginCommand, Result<LoginResponse>> handler,
        CancellationToken cancellationToken)
    {
        // La IP no viene del body: la pone el servidor para que quede fiel en auditoría.
        var withIp = command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() };

        var result = await handler.Handle(withIp, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>Datos del usuario autenticado, leídos de la DB (no de los claims del token).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(
        [FromServices] IQueryHandler<GetCurrentUserQuery, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new GetCurrentUserQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cambio de la contraseña PROPIA (requiere la actual). Es el camino para que un
    /// usuario creado a mano deje de usar la contraseña que le puso el admin.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        [FromServices] ICommandHandler<ChangePasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(
            command with
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return NoContent();
    }

    private Guid? GetCurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
