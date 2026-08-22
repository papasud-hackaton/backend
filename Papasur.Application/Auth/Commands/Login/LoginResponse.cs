using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Auth.Commands.Login;

/// <summary>Respuesta del login (contrato §1): { user, token }.</summary>
public sealed record LoginResponse(UserDto User, string Token, DateTime ExpiresAt);
