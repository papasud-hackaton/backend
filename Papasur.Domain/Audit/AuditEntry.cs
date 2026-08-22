using Papasur.Domain.Users;

namespace Papasur.Domain.Audit;

/// <summary>
/// Registro de auditoría: qué hizo un agente, sobre qué entidad y cuándo.
/// Es INMUTABLE y está DESNORMALIZADO a propósito (contrato §6): guarda el nombre y el rol
/// que tenía el actor EN ESE MOMENTO, no una FK. Si después cambia de nombre o de rol, el
/// registro histórico tiene que seguir diciendo lo que era entonces.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    /// <summary>Agente que ejecutó la acción (FK restrict: un usuario con auditoría no se borra).</summary>
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Nombre del actor al momento del hecho. Copiado, no derivado.</summary>
    public string ActorName { get; set; } = string.Empty;

    /// <summary>Rol del actor al momento del hecho. Copiado, no derivado.</summary>
    public string ActorRole { get; set; } = string.Empty;

    /// <summary>Acción ejecutada (enum cerrado, ver AuditActions).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Tipo de entidad afectada ("user", "form", "document").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Identificador de la entidad afectada, si aplica.</summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Cambios en JSON: [{ "field": "status", "from": "draft", "to": "submitted" }].
    /// Es lo que le permite al front mostrar qué cambió sin adivinar.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>Detalle libre y corto. NUNCA incluir contraseñas ni datos sensibles.</summary>
    public string? Detail { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Enum CERRADO de acciones auditables (contrato §6). Agregar una acción es agregar
/// una constante acá, nunca un literal suelto en un handler.
/// </summary>
public static class AuditActions
{
    public const string UserLogin = "user.login";

    public const string UserLogout = "user.logout";

    public const string UserPasswordResetRequested = "user.password_reset_requested";

    public const string UserPasswordChanged = "user.password_changed";

    public const string UserProfileUpdated = "user.profile_updated";

    public const string UserCreated = "user.created";

    public const string UserUpdated = "user.updated";

    public const string UserDeactivated = "user.deactivated";

    public const string UserRoleChanged = "user.role_changed";

    public const string FormCreated = "form.created";

    public const string FormUpdated = "form.updated";

    public const string FormSubmitted = "form.submitted";

    public const string FormApproved = "form.approved";

    public const string FormChangesRequested = "form.changes_requested";

    public const string FormIssued = "form.issued";

    public const string FormCancelled = "form.cancelled";

    public const string FormReopened = "form.reopened";

    public const string DocumentGenerated = "document.generated";

    /// <summary>Confirmación humana de un documento del copiloto (vertical de trazabilidad).</summary>
    public const string DocumentConfirmed = "document.confirmed";

    public const string DocumentDownloaded = "document.downloaded";

    public const string SettingsUpdated = "settings.updated";

    public static readonly string[] All =
    [
        UserLogin, UserLogout, UserPasswordResetRequested, UserPasswordChanged,
        UserProfileUpdated, UserCreated, UserUpdated, UserDeactivated, UserRoleChanged,
        FormCreated, FormUpdated, FormSubmitted, FormApproved, FormChangesRequested,
        FormIssued, FormCancelled, FormReopened,
        DocumentGenerated, DocumentConfirmed, DocumentDownloaded, SettingsUpdated,
    ];

    public static bool Exists(string action) => All.Contains(action);
}

/// <summary>Tipos de entidad auditables, tal como los espera el front.</summary>
public static class AuditEntityTypes
{
    public const string User = "user";

    public const string Form = "form";

    public const string Document = "document";

    public const string Settings = "settings";
}
