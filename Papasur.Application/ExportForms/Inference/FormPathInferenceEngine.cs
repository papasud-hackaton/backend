using System.Globalization;
using Papasur.Application.Customers;
using Papasur.Application.Documentos.Inference;
using Papasur.Domain.ExportForms;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.ExportForms.Inference;

/// <summary>
/// Implementación determinística del motor de ámbito formulario — portada de requirementsEngine.ts.
///
/// Regla de `items[]`: el campo vale para TODAS las líneas. Si alguna no lo tiene, no se da por
/// resuelto y se dice cuántas faltan; si todas coinciden se devuelve el valor, y si difieren se
/// informa que hay varios. Nunca se inventa un valor ni se elige uno "representativo".
/// </summary>
public sealed class FormPathInferenceEngine(OrganizationProfile organization) : IFormInferenceEngine
{
    private const string ItemsPrefix = "items[].";

    /// <summary>Los documentos no pueden depender de la configuración regional del servidor.</summary>
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public CampoInferido? Infer(string? path, ExportForm form, Cliente? customer)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var route = path.Trim();

        var value = route.StartsWith(ItemsPrefix, StringComparison.Ordinal)
            ? ResolveItems(route[ItemsPrefix.Length..], form)
            : ResolveScalar(route, form, customer);

        return string.IsNullOrWhiteSpace(value) ? null : new CampoInferido(value, route);
    }

    private string? ResolveScalar(string route, ExportForm form, Cliente? customer)
    {
        var totals = FormCalculations.ComputeTotals(form.Items);

        return route switch
        {
            "organization.legalName" => organization.LegalName,
            "organization.taxId" => organization.TaxId,
            "organization.countryName" => organization.CountryName,
            "organization.province" => organization.Province,

            "customer.name" => customer?.Nombre,
            "customer.taxId" => customer?.TaxId,
            "customer.address" => customer?.Address,
            "customer.city" => customer?.City,
            "customer.countryName" => customer?.Pais ?? Countries.NameOf(form.DestinationCountryCode),

            "form.incoterm" => form.Incoterm,
            "form.currency" => form.Currency,
            "form.portOfLoading" => Blank(form.PortOfLoading),
            "form.portOfDischarge" => Blank(form.PortOfDischarge),
            "form.paymentTerms" => Blank(form.PaymentTerms),
            "form.validUntil" => form.ValidUntil?.ToString("yyyy-MM-dd", Invariant),
            "form.notes" => Blank(form.Notes),
            "form.code" => form.Code,
            "form.totals.totalKg" => Num(totals.TotalKg),
            "form.totals.totalPackages" => totals.TotalPackages.ToString(Invariant),
            "form.totals.totalAmount" => Num(totals.TotalAmount),

            _ => null,
        };
    }

    private static string? ResolveItems(string itemRoute, ExportForm form)
    {
        if (form.Items.Count == 0)
        {
            return null;
        }

        var values = form.Items
            .OrderBy(i => i.Position)
            .Select(i => ValueOf(itemRoute, i))
            .ToList();

        var missing = values.Count(string.IsNullOrWhiteSpace);

        if (missing > 0)
        {
            return $"Falta en {missing} de {values.Count} líneas";
        }

        var unique = values.Distinct().ToList();

        return unique.Count == 1 ? unique[0] : $"{unique.Count} valores distintos";
    }

    private static string? ValueOf(string route, ExportFormItem item) => route switch
    {
        "traceability.lotCode" => item.Traceability.LotCode,
        "traceability.species" => item.Traceability.Species,
        "traceability.variety" => item.Traceability.Variety,
        "traceability.category" => item.Traceability.Category,
        "traceability.cropYear" => item.Traceability.CropYear.ToString(Invariant),
        "traceability.locationCode" => item.Traceability.LocationCode,
        "traceability.germinationRate" => Num(item.Traceability.GerminationRate),
        "traceability.purity" => Num(item.Traceability.Purity),
        "traceability.inaseRegistration" => item.Traceability.InaseRegistration,
        "quantityKg" => Num(item.QuantityKg),
        "packagingType" => item.PackagingType,
        "packagesCount" => item.PackagesCount.ToString(Invariant),
        "unitPrice" => Num(item.UnitPrice),
        "lineTotal" => Num(item.LineTotal),
        _ => null,
    };

    private static string? Num(decimal? value)
        => value?.ToString("0.####", Invariant);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
