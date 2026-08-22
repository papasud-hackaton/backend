namespace Papasur.Application.ExportForms.Queries.GetFormById;

/// <summary>Advertencia en el shape del contrato (§5).</summary>
public sealed record FormWarningDto(string Code, string Severity, string Message, string? Field);

/// <summary>Trazabilidad congelada de una línea.</summary>
public sealed record TraceabilitySnapshotDto(
    string LotCode,
    string Species,
    string Variety,
    string Category,
    int CropYear,
    string LocationCode,
    decimal? GerminationRate,
    decimal? Purity,
    string? InaseRegistration,
    DateTime CapturedAt);

public sealed record ExportFormItemDto(
    Guid Id,
    Guid LotId,
    decimal QuantityKg,
    string PackagingType,
    int PackagesCount,
    decimal UnitPrice,
    decimal LineTotal,
    TraceabilitySnapshotDto Traceability,
    IReadOnlyList<FormWarningDto> Warnings);

public sealed record ExportFormTotalsDto(decimal TotalKg, int TotalPackages, decimal TotalAmount);

/// <summary>Documento generado para el envío (contrato §5).</summary>
public sealed record GeneratedDocumentDto(
    Guid Id,
    Guid FormId,
    string Type,
    string Status,
    DateTime? GeneratedAt,
    Guid? GeneratedBy);

/// <summary>
/// Formulario completo tal como lo consume el front (contrato §5). Lo derivado —totales y
/// advertencias— se calcula acá y NUNCA se toma del cliente.
/// </summary>
public sealed record ExportFormDto(
    Guid Id,
    string Code,
    string Status,
    int Version,
    Guid? CustomerId,
    string DestinationCountryCode,
    string PortOfLoading,
    string PortOfDischarge,
    string Incoterm,
    string Currency,
    string? PaymentTerms,
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyList<ExportFormItemDto> Items,
    ExportFormTotalsDto Totals,
    IReadOnlyList<GeneratedDocumentDto> Documents,
    IReadOnlyList<FormWarningDto> Warnings,
    IReadOnlyDictionary<string, string> RequirementValues,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    DateTime? IssuedAt);
