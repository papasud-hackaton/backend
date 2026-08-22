namespace Papasur.Application.Auth.Commands.Login;

/// <summary>Respuesta del login: el JWT y los datos mínimos del usuario para el front.</summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Name,
    string Email,
    string Role);
