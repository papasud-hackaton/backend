using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Papasur.Application.Auth.Ports;
using Papasur.Domain.Users;

namespace Papasur.Infrastructure.Auth;

/// <summary>
/// Emisión de JWT HS256 con la misma key/issuer/audience que valida la API.
/// El rol viaja como claim ClaimTypes.Role, que es lo que consume [AuthorizeRoles(...)].
/// </summary>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : ITokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Generate(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.SymmetricKey))
        {
            throw new InvalidOperationException("Falta Jwt__SymmetricKey para emitir tokens.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SymmetricKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
                new Claim("employee_number", user.EmployeeNumber),
            ]),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }
}
