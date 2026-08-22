namespace Papasur.Application.ExportForms.Queries.GetForms;

/// <summary>
/// Versión aplanada para listados (contrato §5): trae el nombre del cliente, el país y el del
/// autor ya resueltos, y la CANTIDAD de advertencias en vez del array. Evita el N+1 en el front.
/// </summary>
public sealed record ExportFormSummaryDto(
    Guid Id,
    string Code,
    string Status,
    string CustomerName,
    string DestinationCountryName,
    decimal TotalKg,
    decimal TotalAmount,
    string Currency,
    Guid CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt,
    int WarningCount);
