using Papasur.Domain.Users;

namespace Papasur.Domain.ExportForms;

/// <summary>
/// Formulario de exportación: UN envío, compuesto por N líneas atadas a lotes reales.
/// Es la entidad central del contrato (§5) y la respuesta al dolor original del cliente
/// (la planilla compartida): tiene dueño único y <see cref="Version"/> para bloqueo optimista.
/// </summary>
public class ExportForm
{
    public Guid Id { get; set; }

    /// <summary>Correlativo por año que asigna el servidor: PF-2026-0001.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Estado del ciclo de vida (ver <see cref="FormStatuses"/>). Sólo cambia por transición.</summary>
    public string Status { get; set; } = FormStatuses.Draft;

    /// <summary>
    /// Sube en CADA escritura. El cliente lo manda como If-Match; si no coincide, 409 y no se escribe.
    /// </summary>
    public int Version { get; set; } = 1;

    public Guid? CustomerId { get; set; }

    public Trazabilidad.Cliente? Customer { get; set; }

    public string DestinationCountryCode { get; set; } = string.Empty;

    public string PortOfLoading { get; set; } = string.Empty;

    public string PortOfDischarge { get; set; } = string.Empty;

    public string Incoterm { get; set; } = Incoterms.Fob;

    public string Currency { get; set; } = Currencies.Usd;

    public string? PaymentTerms { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Campos de requisitos que el sistema no pudo derivar y cargó una persona, serializados
    /// como JSON { clave: valor }. Alimentan la generación de documentos.
    /// </summary>
    public string? RequirementValues { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Obligatorio al pedir cambios: es lo que el agente tiene que corregir.</summary>
    public string? ReviewNotes { get; set; }

    public DateTime? IssuedAt { get; set; }

    public ICollection<ExportFormItem> Items { get; set; } = [];

    /// <summary>Documentos generados para este envío (proyección sobre la trazabilidad congelada).</summary>
    public ICollection<Documentos.DocumentoExportacion> Documents { get; set; } = [];
}

/// <summary>Estados del formulario (contrato §5). Enum cerrado.</summary>
public static class FormStatuses
{
    public const string Draft = "draft";

    public const string Submitted = "submitted";

    public const string ChangesRequested = "changes_requested";

    public const string Approved = "approved";

    public const string Issued = "issued";

    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Draft, Submitted, ChangesRequested, Approved, Issued, Cancelled];

    public static bool Exists(string status) => All.Contains(status);

    /// <summary>Sólo estos dos se pueden editar (contrato §5).</summary>
    public static bool IsEditable(string status) => status is Draft or ChangesRequested;
}

/// <summary>Acciones de la máquina de estados. Enum cerrado.</summary>
public static class FormActions
{
    public const string Submit = "submit";

    public const string RequestChanges = "request_changes";

    public const string Approve = "approve";

    public const string Issue = "issue";

    public const string Cancel = "cancel";

    public const string Reopen = "reopen";

    public static readonly string[] All = [Submit, RequestChanges, Approve, Issue, Cancel, Reopen];

    public static bool Exists(string action) => All.Contains(action);
}

public static class Incoterms
{
    public const string Fob = "FOB";

    public static readonly string[] All = ["EXW", "FCA", Fob, "CFR", "CIF", "CPT", "CIP", "DAP", "DDP"];

    public static bool Exists(string incoterm) => All.Contains(incoterm);
}

public static class Currencies
{
    public const string Usd = "USD";

    public static readonly string[] All = [Usd, "EUR", "ARS"];

    public static bool Exists(string currency) => All.Contains(currency);
}
