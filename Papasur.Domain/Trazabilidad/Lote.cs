namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Lote de papa semilla: unidad CENTRAL de trazabilidad. La documentación de exportación es una
/// proyección sobre estos datos (nunca al revés). Se identifica por el número de lote de la
/// planilla de movimientos (columna "Lote": 241, 300, 910...).
/// </summary>
public class Lote
{
    public Guid Id { get; set; }

    /// <summary>Número / código de lote de la operación (columna "Lote").</summary>
    public string Codigo { get; set; } = string.Empty;

    public Guid VariedadId { get; set; }

    public Variedad Variedad { get; set; } = null!;

    /// <summary>Campo de origen (del plano), si se conoce.</summary>
    public Guid? CampoId { get; set; }

    public Campo? Campo { get; set; }

    /// <summary>Categoría de semilla (fiscalizada, certificada...), si aplica.</summary>
    public string? Categoria { get; set; }

    /// <summary>Superficie sembrada en hectáreas (hoja "Stocks": producción por lote/chacra), si se conoce.</summary>
    public decimal? SuperficieHa { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
