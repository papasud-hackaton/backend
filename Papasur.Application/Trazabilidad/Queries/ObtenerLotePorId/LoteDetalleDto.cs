namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;

/// <summary>Detalle de un lote con toda su trazabilidad (movimientos incluidos) para la generación de documentos.</summary>
public sealed record LoteDetalleDto(
    Guid Id,
    string Codigo,
    Guid VariedadId,
    string Variedad,
    Guid? CampoId,
    string? Campo,
    string? Establecimiento,
    string? Categoria,
    decimal? SuperficieHa,
    DateTime CreatedAt,
    IReadOnlyList<MovimientoDto> Movimientos);
