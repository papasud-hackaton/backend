namespace Papasur.Domain.Users;

/// <summary>
/// Usuario del sistema. La contraseña NUNCA se guarda en claro: sólo el hash
/// (PBKDF2-SHA256 con salt por usuario, ver IPasswordHasher).
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Nombre y apellido del usuario.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Correo, único; es el identificador con el que se hace login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (formato "iteraciones.salt.hash" en base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Legajo del empleado, único.</summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    /// <summary>Baja lógica: un usuario inactivo no puede autenticarse.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
