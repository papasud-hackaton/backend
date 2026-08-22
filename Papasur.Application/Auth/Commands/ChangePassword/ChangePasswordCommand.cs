using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Cambio de la contraseña PROPIA (PATCH /auth/password). El UserId lo pone el controller
/// desde el JWT: nunca viene del body, así nadie puede cambiarle la clave a otro por acá.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Result>
{
    public Actor? Actor { get; init; }
}
