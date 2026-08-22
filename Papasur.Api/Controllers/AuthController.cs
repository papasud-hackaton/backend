using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Auth.Commands.Login;

namespace Papasur.Api.Controllers;

/// <summary>Autenticación: emisión del JWT.</summary>
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
}
