using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Users.Commands.SetUserActive;

/// <summary>
/// Alta/baja LÓGICA de un usuario. Los usuarios no se borran: la auditoría los referencia
/// y debe seguir siendo legible.
/// </summary>
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand<Result>
{
    public Guid? PerformedByUserId { get; init; }

    public string? IpAddress { get; init; }
}
