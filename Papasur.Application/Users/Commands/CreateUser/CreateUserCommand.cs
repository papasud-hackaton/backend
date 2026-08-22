using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Users.Commands.CreateUser;

/// <summary>
/// Alta de usuario. PerformedByUserId lo completa el controller con el usuario del JWT
/// (no viene del body) y queda registrado en auditoría.
/// </summary>
public sealed record CreateUserCommand(
    string Name,
    string Email,
    string Password,
    string EmployeeNumber,
    int RoleId) : ICommand<Result<Guid>>
{
    public Guid? PerformedByUserId { get; init; }

    public string? IpAddress { get; init; }
}
