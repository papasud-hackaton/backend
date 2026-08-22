using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.ResetPassword;

/// <summary>Define la contraseña usando el token del enlace (recuperación o invitación).</summary>
public sealed record ResetPasswordCommand(string Token, string Password) : ICommand<Result>
{
    public string? IpAddress { get; init; }
}
