using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Auth.Commands.Logout;

/// <summary>
/// Cierre de sesión. El JWT es stateless, así que no se invalida nada del lado del server:
/// el valor de este endpoint es dejar el rastro en auditoría.
/// </summary>
public sealed record LogoutCommand(Actor Actor) : ICommand<Result>;
