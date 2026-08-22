namespace Papasur.Domain.Users;

/// <summary>
/// Usuario del sistema. La contraseña NUNCA se guarda en claro: sólo el hash
/// (PBKDF2-SHA256 con salt por usuario, ver IPasswordHasher).
/// Los nombres de campo siguen el contrato de API (inglés): employeeId, no legajo.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Correo, único; es el identificador con el que se hace login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña (formato "iteraciones.salt.hash" en base64).
    /// Vacío mientras el usuario está invitado y todavía no definió una.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Legajo del empleado, único.</summary>
    public string EmployeeId { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    /// <summary>invited | active | inactive (ver UserStatuses). Los usuarios NO se borran.</summary>
    public string Status { get; set; } = UserStatuses.Active;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Nombre completo, para desnormalizar en auditoría y listados.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Sólo un usuario activo puede autenticarse.</summary>
    public bool IsActive => Status == UserStatuses.Active;
}

/// <summary>
/// Estados del usuario. "invited" es el alta hecha por un admin: existe, todavía no
/// definió contraseña y no puede entrar hasta activarse.
/// </summary>
public static class UserStatuses
{
    public const string Invited = "invited";

    public const string Active = "active";

    public const string Inactive = "inactive";

    public static readonly string[] All = [Invited, Active, Inactive];

    public static bool Exists(string status) => All.Contains(status);
}
