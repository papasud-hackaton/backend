using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Cambio de la contraseña PROPIA. UserId lo pone el controller desde el JWT: nunca
/// viene del body, así nadie puede cambiarle la contraseña a otro por esta vía.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Result>
{
    public Guid UserId { get; init; }

    public string? IpAddress { get; init; }
}
