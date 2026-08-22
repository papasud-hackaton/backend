using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Users.Commands.SetUserStatus;

/// <summary>Alta/baja lógica. Los usuarios nunca se borran: la auditoría los referencia.</summary>
public sealed record SetUserStatusCommand(Guid UserId, string Status) : ICommand<Result<UserDto>>
{
    public Actor? Actor { get; init; }
}
