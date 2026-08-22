using System.Text.Json;
using Papasur.Application.Customers;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Application.ExportForms.Queries.GetForms;
using Papasur.Application.Lots.Ports;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms;

/// <summary>Estado de un documento generado (contrato §5, DocumentStatus del front).</summary>
public static class DocumentStatuses
{
    public const string Pending = "pending";

    public const string Generated = "generated";

    public const string Failed = "failed";
}

/// <summary>
/// Arma la respuesta de un formulario: totales y advertencias SIEMPRE calculados acá, nunca
/// tomados del cliente (contrato §0.2) y nunca guardados en la base.
///
/// Que las advertencias se recalculen en cada lectura no es un detalle: el saldo del lote se
/// mueve, y el valor del producto es que el número que se ve al armar el documento sea el de
/// ese momento. En un formulario ya emitido o anulado no se calculan: el control era antes.
/// </summary>
public sealed class FormAssembler(ILotProjectionRepository lots)
{
    /// <summary>Estados en los que el control de stock todavía tiene sentido.</summary>
    private static readonly string[] WarnableStatuses =
    [
        FormStatuses.Draft, FormStatuses.ChangesRequested, FormStatuses.Submitted, FormStatuses.Approved,
    ];

    public async Task<ExportFormDto> ToDtoAsync(ExportForm form, CancellationToken cancellationToken)
    {
        var stock = await StockFor([form], cancellationToken);

        return ToDto(form, stock, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<ExportFormSummaryDto>> ToSummariesAsync(
        IReadOnlyList<ExportForm> forms,
        CancellationToken cancellationToken)
    {
        var stock = await StockFor(forms, cancellationToken);
        var now = DateTime.UtcNow;

        return [.. forms.Select(f => ToSummary(f, stock, now))];
    }

    /// <summary>Las advertencias vivas de un formulario: es lo que decide si se puede enviar a revisión.</summary>
    public async Task<IReadOnlyList<FormWarning>> WarningsAsync(ExportForm form, CancellationToken cancellationToken)
    {
        var stock = await StockFor([form], cancellationToken);

        return Warnings(form, stock, DateTime.UtcNow);
    }

    private async Task<Dictionary<Guid, LotStock>> StockFor(
        IReadOnlyList<ExportForm> forms,
        CancellationToken cancellationToken)
    {
        var lotIds = forms.SelectMany(f => f.Items).Select(i => i.LotId).Distinct().ToArray();

        if (lotIds.Length == 0)
        {
            return [];
        }

        var found = await lots.GetByIdsAsync(lotIds, cancellationToken);

        return found.ToDictionary(
            l => l.Id,
            l => new LotStock(l.Code, l.Status, l.AvailableKg, l.LastInventoryAt, l.GerminationRate, l.InaseRegistration));
    }

    private static IReadOnlyList<FormWarning> ItemWarnings(
        ExportFormItem item,
        IReadOnlyDictionary<Guid, LotStock> stock,
        DateTime now)
        => stock.TryGetValue(item.LotId, out var lot)
            ? FormCalculations.WarningsForLot(lot, item.QuantityKg, now)
            : [];

    private static IReadOnlyList<FormWarning> Warnings(
        ExportForm form,
        IReadOnlyDictionary<Guid, LotStock> stock,
        DateTime now)
    {
        if (!WarnableStatuses.Contains(form.Status))
        {
            return [];
        }

        var warnings = form.Items.SelectMany(i => ItemWarnings(i, stock, now)).ToList();
        var mixed = FormCalculations.MixedCategoryWarning(form.Items.Select(i => i.Traceability.Category));

        if (mixed is not null)
        {
            warnings.Add(mixed);
        }

        return warnings;
    }

    private static ExportFormDto ToDto(
        ExportForm form,
        IReadOnlyDictionary<Guid, LotStock> stock,
        DateTime now)
    {
        var warnable = WarnableStatuses.Contains(form.Status);
        var totals = FormCalculations.ComputeTotals(form.Items);

        var items = form.Items
            .OrderBy(i => i.Position)
            .Select(i => new ExportFormItemDto(
                i.Id,
                i.LotId,
                i.QuantityKg,
                i.PackagingType,
                i.PackagesCount,
                i.UnitPrice,
                i.LineTotal,
                new TraceabilitySnapshotDto(
                    i.Traceability.LotCode,
                    i.Traceability.Species,
                    i.Traceability.Variety,
                    i.Traceability.Category,
                    i.Traceability.CropYear,
                    i.Traceability.LocationCode,
                    i.Traceability.GerminationRate,
                    i.Traceability.Purity,
                    i.Traceability.InaseRegistration,
                    i.Traceability.CapturedAt),
                warnable ? [.. ItemWarnings(i, stock, now).Select(ToDto)] : []))
            .ToList();

        var documents = form.Documents
            .Select(d => new GeneratedDocumentDto(
                d.Id,
                form.Id,
                d.PlantillaDocumento?.Codigo ?? string.Empty,
                DocumentStatuses.Generated,
                d.CreatedAt,
                d.CreatedByUserId))
            .ToList();

        return new ExportFormDto(
            form.Id,
            form.Code,
            form.Status,
            form.Version,
            form.CustomerId,
            form.DestinationCountryCode,
            form.PortOfLoading,
            form.PortOfDischarge,
            form.Incoterm,
            form.Currency,
            form.PaymentTerms,
            form.ValidUntil,
            form.Notes,
            items,
            new ExportFormTotalsDto(totals.TotalKg, totals.TotalPackages, totals.TotalAmount),
            documents,
            [.. Warnings(form, stock, now).Select(ToDto)],
            ReadRequirementValues(form.RequirementValues),
            form.CreatedByUserId,
            form.CreatedAt,
            form.UpdatedAt,
            form.SubmittedAt,
            form.ReviewedByUserId,
            form.ReviewedAt,
            form.ReviewNotes,
            form.IssuedAt);
    }

    private static ExportFormSummaryDto ToSummary(
        ExportForm form,
        IReadOnlyDictionary<Guid, LotStock> stock,
        DateTime now)
    {
        var totals = FormCalculations.ComputeTotals(form.Items);

        return new ExportFormSummaryDto(
            form.Id,
            form.Code,
            form.Status,
            form.Customer?.Nombre ?? string.Empty,
            form.Customer?.Pais ?? Countries.NameOf(form.DestinationCountryCode),
            totals.TotalKg,
            totals.TotalAmount,
            form.Currency,
            form.CreatedByUserId,
            form.CreatedByUser is null
                ? string.Empty
                : $"{form.CreatedByUser.FirstName} {form.CreatedByUser.LastName}".Trim(),
            form.CreatedAt,
            form.UpdatedAt,
            form.SubmittedAt,
            Warnings(form, stock, now).Count);
    }

    private static FormWarningDto ToDto(FormWarning warning)
        => new(warning.Code, warning.Severity, warning.Message, warning.Field);

    /// <summary>Los valores de requisitos viajan como JSON plano; si está corrupto, se devuelve vacío.</summary>
    public static IReadOnlyDictionary<string, string> ReadRequirementValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    public static string? WriteRequirementValues(IReadOnlyDictionary<string, string>? values)
        => values is null || values.Count == 0 ? null : JsonSerializer.Serialize(values);
}
