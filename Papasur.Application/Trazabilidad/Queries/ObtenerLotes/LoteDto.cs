namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotes;

/// <summary>Proyección de lote para listados (incluye nombres de variedad y campo, no las entidades).</summary>
public sealed record LoteDto(
    Guid Id,
    string Codigo,
    Guid VariedadId,
    string Variedad,
    Guid? CampoId,
    string? Campo,
    string? Categoria,
    decimal? SuperficieHa,
    int CantidadMovimientos,
    DateTime CreatedAt);
