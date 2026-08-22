namespace Papasur.Application.ExportForms.Commands;

/// <summary>
/// Línea tal como la manda el cliente. Sólo se aceptan estos campos: la trazabilidad la congela
/// el servidor leyendo el lote, y los derivados (bultos, total de línea) los calcula (contrato §0.2).
/// </summary>
public sealed record FormItemInput(
    Guid LotId,
    decimal QuantityKg,
    string PackagingType,
    decimal UnitPrice);

/// <summary>Campos editables de un formulario (lista blanca del contrato §0.2).</summary>
public sealed record FormFieldsInput(
    Guid? CustomerId = null,
    string? DestinationCountryCode = null,
    string? PortOfLoading = null,
    string? PortOfDischarge = null,
    string? Incoterm = null,
    string? Currency = null,
    string? PaymentTerms = null,
    DateTime? ValidUntil = null,
    string? Notes = null,
    IReadOnlyList<FormItemInput>? Items = null,
    IReadOnlyDictionary<string, string>? RequirementValues = null);
