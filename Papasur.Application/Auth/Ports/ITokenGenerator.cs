using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Ports;

/// <summary>Token JWT emitido para un usuario autenticado.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>
/// Emisión de JWT. Implementado en Infrastructure (JwtTokenGenerator, HS256).
/// </summary>
public interface ITokenGenerator
{
    AccessToken Generate(User user);
}
