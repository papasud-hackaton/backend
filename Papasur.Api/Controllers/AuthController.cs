using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Auth.Commands.ChangePassword;
using Papasur.Application.Auth.Commands.ForgotPassword;
using Papasur.Application.Auth.Commands.Login;
using Papasur.Application.Auth.Commands.Logout;
using Papasur.Application.Auth.Commands.ResetPassword;
using Papasur.Application.Auth.Queries.GetCurrentUser;
using Papasur.Application.Users.Commands.UpdateUser;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Api.Controllers;

/// <summary>
/// Autenticación (contrato §1). NO hay registro público: los usuarios los crea un admin.
/// </summary>
[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    /// <summary>Login por correo + contraseña. Devuelve { user, token }.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, Result<LoginResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new LoginCommand(request.Email, request.Password)
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        if (result.IsFailure)
        {
            // Cuenta desactivada: 403. Cualquier otro fallo: 401 con el MISMO mensaje.
            var status = result.Error.Code == "Auth.Disabled"
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;

            return Fail(status, result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>Usuario del token. El front lo llama al arrancar para validar la sesión guardada.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(
        [FromServices] IQueryHandler<GetCurrentUserQuery, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Fail(StatusCodes.Status401Unauthorized, "Tu sesión no es válida.", "unauthenticated");
        }

        var result = await handler.Handle(new GetCurrentUserQuery(userId), cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status404NotFound, result.Error)
            : Ok(result.Value);
    }

    /// <summary>Cierre de sesión: el JWT es stateless, esto deja el rastro en auditoría.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromServices] ICommandHandler<LogoutCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is { } actor)
        {
            await handler.Handle(new LogoutCommand(actor), cancellationToken);
        }

        return NoContent();
    }

    /// <summary>Pedido de recuperación. Responde 204 SIEMPRE, exista o no la cuenta.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] ICommandHandler<ForgotPasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new ForgotPasswordCommand(request.Email)
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        return NoContent();
    }

    /// <summary>Define la contraseña con el token del enlace (recuperación o invitación).</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ICommandHandler<ResetPasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ResetPasswordCommand(request.Token, request.Password)
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            },
            cancellationToken);

        if (result.IsFailure)
        {
            // Token vencido o ya usado: 410, que es lo que el front espera para ofrecer pedir otro.
            var status = result.Error.Code == "Auth.TokenExpired"
                ? StatusCodes.Status410Gone
                : StatusCodes.Status400BadRequest;

            return Fail(status, result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Edición del perfil PROPIO. No incluye el rol a propósito: eso sólo lo cambia un admin
    /// desde /users/{id}.
    /// </summary>
    [HttpPatch("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] ICommandHandler<UpdateUserCommand, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Fail(StatusCodes.Status401Unauthorized, "Tu sesión no es válida.", "unauthenticated");
        }

        var result = await handler.Handle(
            new UpdateUserCommand(userId)
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Actor = CurrentActor,
            },
            cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : Ok(result.Value);
    }

    /// <summary>Cambio de la contraseña propia (requiere la actual).</summary>
    [HttpPatch("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword) { Actor = CurrentActor },
            cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : NoContent();
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UpdateProfileRequest(string? FirstName, string? LastName, string? Phone);
