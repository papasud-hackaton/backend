using Papasur.Domain.Users;

namespace Papasur.Domain.ExportForms;

/// <summary>Resultado de evaluar una transición: o se puede, o se explica por qué no.</summary>
public sealed record TransitionCheck(bool Allowed, string Reason = "")
{
    public static readonly TransitionCheck Ok = new(true);

    public static TransitionCheck No(string reason) => new(false, reason);
}

/// <summary>Regla de una acción: de qué estados sale, a cuál llega, qué roles y qué exige.</summary>
public sealed record TransitionRule(
    string[] From,
    string To,
    string[] Roles,
    string Label,
    bool OwnerOnly = false,
    string? Requires = null);

/// <summary>
/// El formulario visto por la máquina de estados. Sólo lo que hace falta para decidir:
/// nada de EF, nada de DTOs.
/// </summary>
public sealed record FormView(
    string Status,
    Guid CreatedByUserId,
    int ItemCount,
    bool HasBlockingWarnings,
    int DocumentCount);

/// <summary>
/// Máquina de estados del formulario (contrato §5) — portada de formStateMachine.ts, que el
/// propio contrato pide portar tal cual en vez de reescribir.
///
/// ÚNICA FUENTE DE VERDAD de las transiciones. El 403 devuelve el motivo real, porque el front
/// lo muestra tal cual.
/// </summary>
public static class FormStateMachine
{
    /// <summary>Texto obligatorio que exige una acción.</summary>
    public const string RequiresReviewNotes = "reviewNotes";

    public const string RequiresReason = "reason";

    public static readonly IReadOnlyDictionary<string, TransitionRule> Transitions =
        new Dictionary<string, TransitionRule>
        {
            [FormActions.Submit] = new(
                [FormStatuses.Draft, FormStatuses.ChangesRequested],
                FormStatuses.Submitted,
                [RoleNames.Agent, RoleNames.Supervisor, RoleNames.Admin],
                "Enviar a revisión",
                OwnerOnly: true),

            [FormActions.RequestChanges] = new(
                [FormStatuses.Submitted],
                FormStatuses.ChangesRequested,
                [RoleNames.Supervisor, RoleNames.Admin],
                "Pedir cambios",
                Requires: RequiresReviewNotes),

            [FormActions.Approve] = new(
                [FormStatuses.Submitted],
                FormStatuses.Approved,
                [RoleNames.Supervisor, RoleNames.Admin],
                "Aprobar"),

            [FormActions.Issue] = new(
                [FormStatuses.Approved],
                FormStatuses.Issued,
                [RoleNames.Supervisor, RoleNames.Admin],
                "Emitir"),

            [FormActions.Cancel] = new(
                [FormStatuses.Draft, FormStatuses.Submitted, FormStatuses.ChangesRequested, FormStatuses.Approved],
                FormStatuses.Cancelled,
                [RoleNames.Agent, RoleNames.Supervisor, RoleNames.Admin],
                "Anular",
                Requires: RequiresReason),

            [FormActions.Reopen] = new(
                [FormStatuses.Approved, FormStatuses.Issued],
                FormStatuses.Draft,
                [RoleNames.Admin],
                "Reabrir",
                Requires: RequiresReason),
        };

    public static TransitionCheck CanTransition(FormView form, string action, Guid userId, string role)
    {
        if (!Transitions.TryGetValue(action, out var rule))
        {
            return TransitionCheck.No("La acción no existe.");
        }

        if (!rule.From.Contains(form.Status))
        {
            return TransitionCheck.No($"No se puede {rule.Label.ToLowerInvariant()} en este estado.");
        }

        if (!rule.Roles.Contains(role))
        {
            return TransitionCheck.No("Tu rol no permite esta acción.");
        }

        var isOwner = form.CreatedByUserId == userId;

        // El agente sólo actúa sobre lo propio. Anular es más estricto todavía:
        // un agente sólo puede anular un borrador suyo.
        if (role == RoleNames.Agent)
        {
            if (!isOwner)
            {
                return TransitionCheck.No("El formulario no es tuyo.");
            }

            if (action == FormActions.Cancel && form.Status != FormStatuses.Draft)
            {
                return TransitionCheck.No("Solo podés anular un borrador propio.");
            }
        }

        if (rule.OwnerOnly && !isOwner && role != RoleNames.Admin && role != RoleNames.Supervisor)
        {
            return TransitionCheck.No("El formulario no es tuyo.");
        }

        if (action == FormActions.Submit)
        {
            if (form.ItemCount == 0)
            {
                return TransitionCheck.No("Agregá al menos una línea.");
            }

            if (form.HasBlockingWarnings)
            {
                return TransitionCheck.No("Resolvé las advertencias bloqueantes.");
            }
        }

        if (action == FormActions.Issue && form.DocumentCount == 0)
        {
            return TransitionCheck.No("Generá la documentación antes de emitir.");
        }

        return TransitionCheck.Ok;
    }

    /// <summary>Sólo draft y changes_requested son editables, y sólo por el dueño o un admin.</summary>
    public static bool IsEditable(string status, Guid createdByUserId, Guid userId, string role)
        => FormStatuses.IsEditable(status) && (createdByUserId == userId || role == RoleNames.Admin);

    public static string NextStatus(string action) => Transitions[action].To;

    /// <summary>Acción de auditoría que registra cada transición (contrato §6).</summary>
    public static string AuditActionFor(string action) => action switch
    {
        FormActions.Submit => Audit.AuditActions.FormSubmitted,
        FormActions.Approve => Audit.AuditActions.FormApproved,
        FormActions.RequestChanges => Audit.AuditActions.FormChangesRequested,
        FormActions.Issue => Audit.AuditActions.FormIssued,
        FormActions.Cancel => Audit.AuditActions.FormCancelled,
        FormActions.Reopen => Audit.AuditActions.FormReopened,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Acción sin auditoría asociada."),
    };
}
