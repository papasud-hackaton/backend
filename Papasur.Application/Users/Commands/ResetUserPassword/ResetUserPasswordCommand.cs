using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Users.Commands.ResetUserPassword;

/// <summary>
/// Reseteo de contraseña hecho por un admin (no requiere la contraseña anterior).
/// UserId viene de la ruta; PerformedByUserId, del JWT.
/// </summary>
public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : ICommand<Result>
{
    public Guid? PerformedByUserId { get; init; }

    public string? IpAddress { get; init; }
}
