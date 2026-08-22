namespace Papasur.Infrastructure.Auth;

/// <summary>
/// Configuración del JWT (sección "Jwt" — env Jwt__SymmetricKey, Jwt__Issuer, ...).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SymmetricKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "papasur";

    public string Audience { get; set; } = "papasur";

    /// <summary>Vigencia del access token en minutos.</summary>
    public int ExpirationMinutes { get; set; } = 480;
}
