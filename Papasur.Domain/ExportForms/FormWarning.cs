namespace Papasur.Domain.ExportForms;

/// <summary>
/// Advertencia sobre una línea o sobre el envío. Se comunica con código Y texto: el front
/// muestra el mensaje tal cual, y decide con el código.
/// </summary>
public sealed record FormWarning(string Code, string Severity, string Message, string? Field = null);

public static class WarningCodes
{
    public const string InsufficientStock = "insufficient_stock";

    public const string StaleInventory = "stale_inventory";

    public const string LotQuarantined = "lot_quarantined";

    public const string GerminationBelowThreshold = "germination_below_threshold";

    public const string MissingTraceabilityField = "missing_traceability_field";

    public const string MixedCategories = "mixed_categories";
}

public static class WarningSeverities
{
    public const string Info = "info";

    public const string Warning = "warning";

    /// <summary>Impide enviar a revisión. Es la única severidad que frena la máquina de estados.</summary>
    public const string Blocking = "blocking";
}

/// <summary>
/// Saldo y calidad de un lote en el momento de armar el documento: lo que el motor de
/// advertencias necesita saber. Es un valor puro, calculado sobre los movimientos reales.
/// </summary>
public sealed record LotStock(
    string Code,
    string Status,
    decimal AvailableKg,
    DateTime? LastInventoryAt,
    decimal? GerminationRate,
    string? InaseRegistration);

/// <summary>Estados posibles de un lote, derivados del saldo y de la cuarentena.</summary>
public static class LotStatuses
{
    public const string Available = "available";

    public const string Reserved = "reserved";

    public const string Quarantined = "quarantined";

    public const string Depleted = "depleted";

    public static readonly string[] All = [Available, Reserved, Quarantined, Depleted];

    public static bool Exists(string status) => All.Contains(status);
}
