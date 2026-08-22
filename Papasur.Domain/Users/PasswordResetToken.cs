namespace Papasur.Domain.Users;

/// <summary>
/// Token de recuperación de contraseña. Se guarda HASHEADO (igual que una contraseña):
/// quien vea la base no puede usarlo para tomar una cuenta.
/// De un solo uso y con vencimiento — vencido devuelve 410 (contrato §1).
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Hash del token que viaja en el enlace, nunca el token en claro.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsable(DateTime now) => UsedAt is null && ExpiresAt > now;
}
