using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.Login;

/// <summary>
/// Login por correo + contraseña. IpAddress la completa el controller (no viene del body).
/// </summary>
public sealed record LoginCommand(string Email, string Password) : ICommand<Result<LoginResponse>>
{
    public string? IpAddress { get; init; }
}
