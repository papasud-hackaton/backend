namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;

/// <summary>Proyección de un movimiento/despacho de un lote.</summary>
public sealed record MovimientoDto(
    Guid Id,
    string Tipo,
    string NumeroRemito,
    DateTime Fecha,
    decimal Kilogramos,
    int? Bolsas,
    decimal? KgPromedio,
    string? Presentacion,
    string? Categoria,
    string? Calibre,
    string? Transportista,
    string? Cliente,
    string? Pais,
    string? Comisionista,
    string? Destino,
    string? Dtv,
    string? Observaciones);
