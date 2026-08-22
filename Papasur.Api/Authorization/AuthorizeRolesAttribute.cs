using Microsoft.AspNetCore.Authorization;

namespace Papasur.Api.Authorization;

/// <summary>
/// Autorización por ARRAY de roles: cada endpoint declara qué roles lo pueden usar.
/// Uso: [AuthorizeRoles(RoleNames.Admin, RoleNames.Supervisor)] — el usuario necesita
/// tener AL MENOS UNO de esos roles (OR). Sin roles, equivale a exigir sólo autenticación.
/// Los roles salen del claim ClaimTypes.Role que emite JwtTokenGenerator.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(params string[] roles)
    {
        AllowedRoles = roles;

        if (roles.Length > 0)
        {
            Roles = string.Join(',', roles);
        }
    }

    /// <summary>Roles permitidos, tal cual fueron declarados en el endpoint.</summary>
    public IReadOnlyList<string> AllowedRoles { get; }
}
