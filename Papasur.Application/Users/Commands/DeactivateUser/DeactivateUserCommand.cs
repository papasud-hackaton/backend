using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Users.Commands.DeactivateUser;

/// <summary>
/// Baja LÓGICA (contrato §2: no hay DELETE). Los formularios históricos tienen que
/// conservar a su autor, así que el usuario nunca se borra.
/// </summary>
public sealed record DeactivateUserCommand(Guid UserId) : ICommand<Result<UserDto>>
{
    public Actor? Actor { get; init; }
}
