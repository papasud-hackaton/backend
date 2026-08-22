using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Users.Commands.CreateUser;

/// <summary>
/// Alta de usuario hecha por un admin. NO se manda contraseña (contrato §2): el usuario
/// nace "invited" y define la suya desde el enlace de invitación.
/// </summary>
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string EmployeeId,
    string Role,
    string? Phone = null) : ICommand<Result<UserDto>>
{
    public Actor? Actor { get; init; }
}
