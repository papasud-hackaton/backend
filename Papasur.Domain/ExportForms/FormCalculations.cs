using System.Globalization;

namespace Papasur.Domain.ExportForms;

/// <summary>Totales derivados del envío. NUNCA se aceptan del cliente: se recalculan acá.</summary>
public sealed record ExportFormTotals(decimal TotalKg, int TotalPackages, decimal TotalAmount);

/// <summary>
/// Cálculos y advertencias del formulario — portados de calculations.ts del front, que es
/// la especificación ejecutable (contrato §0).
///
/// El motor de advertencias es el corazón del producto: adelanta el descubrimiento del desvío
/// al momento de armar el documento, en vez de al entregarle el pedido al cliente.
/// </summary>
public static class FormCalculations
{
    /// <summary>Umbrales del negocio. TODO: confirmarlos con Papasud.</summary>
    public const int StaleInventoryDays = 90;

    public const decimal MinGermination = 85m;

    /// <summary>Los mensajes los lee una persona en español; los números van con separador local.</summary>
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");

    public static int PackagesFor(string packagingType, decimal quantityKg)
    {
        var weight = PackagingTypes.WeightOf(packagingType);

        return weight is null or 0 ? 1 : (int)Math.Ceiling(quantityKg / weight.Value);
    }

    public static decimal LineTotal(decimal quantityKg, decimal unitPrice)
        => Math.Round(quantityKg * unitPrice, 2, MidpointRounding.AwayFromZero);

    public static int? DaysSinceInventory(DateTime? lastInventoryAt, DateTime now)
        => lastInventoryAt is null ? null : (int)Math.Floor((now - lastInventoryAt.Value).TotalDays);

    /// <summary>Advertencias de una línea (contrato §5, plan §8.4 paso 2).</summary>
    public static IReadOnlyList<FormWarning> WarningsForLot(LotStock lot, decimal quantityKg, DateTime now)
    {
        var warnings = new List<FormWarning>();

        if (lot.Status == LotStatuses.Quarantined)
        {
            warnings.Add(new FormWarning(
                WarningCodes.LotQuarantined,
                WarningSeverities.Blocking,
                $"El lote {lot.Code} está en cuarentena."));
        }

        if (quantityKg > lot.AvailableKg)
        {
            warnings.Add(new FormWarning(
                WarningCodes.InsufficientStock,
                WarningSeverities.Blocking,
                $"El lote {lot.Code} tiene {lot.AvailableKg.ToString("#,##0.###", Es)} kg disponibles y se pidieron {quantityKg.ToString("#,##0.###", Es)} kg.",
                "quantityKg"));
        }

        var age = DaysSinceInventory(lot.LastInventoryAt, now);

        if (age is > StaleInventoryDays)
        {
            warnings.Add(new FormWarning(
                WarningCodes.StaleInventory,
                WarningSeverities.Warning,
                $"El último inventario del lote {lot.Code} es de hace {age} días."));
        }

        if (lot.GerminationRate is { } germination && germination < MinGermination)
        {
            warnings.Add(new FormWarning(
                WarningCodes.GerminationBelowThreshold,
                WarningSeverities.Warning,
                $"Poder germinativo del lote {lot.Code}: {germination.ToString("0.##", Es)} %, por debajo del {MinGermination.ToString("0.##", Es)} %."));
        }

        if (string.IsNullOrWhiteSpace(lot.InaseRegistration))
        {
            warnings.Add(new FormWarning(
                WarningCodes.MissingTraceabilityField,
                WarningSeverities.Warning,
                $"El lote {lot.Code} no tiene registro INASE cargado.",
                "inaseRegistration"));
        }

        return warnings;
    }

    /// <summary>Mezclar categorías en un mismo envío complica la certificación.</summary>
    public static FormWarning? MixedCategoryWarning(IEnumerable<string> itemCategories)
    {
        var categories = itemCategories.Distinct().Count();

        return categories <= 1
            ? null
            : new FormWarning(
                WarningCodes.MixedCategories,
                WarningSeverities.Info,
                $"El envío mezcla {categories} categorías de semilla.");
    }

    public static bool HasBlocking(IEnumerable<FormWarning> warnings)
        => warnings.Any(w => w.Severity == WarningSeverities.Blocking);

    public static ExportFormTotals ComputeTotals(IEnumerable<ExportFormItem> items)
    {
        var totalKg = 0m;
        var totalPackages = 0;
        var totalAmount = 0m;

        foreach (var item in items)
        {
            totalKg += item.QuantityKg;
            totalPackages += item.PackagesCount;
            totalAmount = Math.Round(totalAmount + item.LineTotal, 2, MidpointRounding.AwayFromZero);
        }

        return new ExportFormTotals(totalKg, totalPackages, totalAmount);
    }
}
