using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Users.Commands.UpdateUser;

/// <summary>
/// Edición parcial de un usuario (contrato §2). Todo opcional: null significa "no tocar".
/// Correo y legajo NO se editan: son identidad y tienen histórico asociado.
/// </summary>
public sealed record UpdateUserCommand(Guid UserId) : ICommand<Result<UserDto>>
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Phone { get; init; }

    public string? Role { get; init; }

    public Actor? Actor { get; init; }
}
