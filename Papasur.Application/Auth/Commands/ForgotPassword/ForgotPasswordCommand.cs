using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.ForgotPassword;

/// <summary>Pedido de recuperación. Responde 204 SIEMPRE, exista o no la cuenta.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand<Result>
{
    public string? IpAddress { get; init; }
}
