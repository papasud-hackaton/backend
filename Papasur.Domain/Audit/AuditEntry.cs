using Papasur.Domain.Users;

namespace Papasur.Domain.Audit;

/// <summary>
/// Registro de auditoría: qué hizo un agente (usuario), sobre qué entidad y cuándo.
/// Relación obligatoria con User (FK restrict: un usuario con auditoría no se borra).
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    /// <summary>Agente que ejecutó la acción.</summary>
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Acción ejecutada (ver AuditActions).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Tipo de entidad afectada (por ejemplo "User", "Document").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Identificador de la entidad afectada, si aplica.</summary>
    public string? EntityId { get; set; }

    /// <summary>Detalle libre y corto. NUNCA incluir contraseñas ni datos sensibles.</summary>
    public string? Detail { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }
}

/// <summary>Acciones auditadas conocidas (string estable, se guarda tal cual).</summary>
public static class AuditActions
{
    public const string Login = "login";

    public const string LoginFailed = "login_failed";

    public const string UserCreated = "user_created";

    public const string UserActivated = "user_activated";

    public const string UserDeactivated = "user_deactivated";

    public const string PasswordChanged = "password_changed";

    public const string PasswordReset = "password_reset";

    public const string DocumentGenerated = "document_generated";

    public const string DocumentConfirmed = "document_confirmed";
}
