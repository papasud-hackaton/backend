namespace Papasur.Application.Lots.Queries.GetLots;

/// <summary>
/// Lote tal como lo consume el front (contrato §3, SeedLot).
///
/// Es una PROYECCIÓN sobre la trazabilidad real: el saldo sale de sumar los movimientos de la
/// planilla, no de un campo guardado. Por eso el front puede confiar en que el número que ve al
/// armar el documento es el de ese momento — que es exactamente lo que hoy falla.
///
/// Los campos de calidad (germinación, pureza, INASE) viajan nulos mientras Papasud no los
/// entregue: el motor de advertencias avisa, no inventa.
///
/// LocationCode es el código visible de la ubicación: es lo que se congela en la trazabilidad
/// de cada línea del formulario.
/// </summary>
public sealed record SeedLotDto(
    Guid Id,
    string Code,
    string Species,
    string Variety,
    string Category,
    int CropYear,
    Guid? LocationId,
    string LocationCode,
    string? Position,
    decimal NetWeightKg,
    decimal AvailableKg,
    decimal ReservedKg,
    decimal? GerminationRate,
    decimal? Purity,
    decimal? Moisture,
    string? Treatment,
    string? InaseRegistration,
    string Status,
    DateTime? LastInventoryAt,
    string? Notes);
