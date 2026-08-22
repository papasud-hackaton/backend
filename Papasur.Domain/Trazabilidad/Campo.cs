namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Campo / finca de origen del lote (del "Plano Santa Ana": establecimiento, finca, pivote y
/// cuadrante donde se sembró). Aporta el origen geográfico que exigen algunos documentos de
/// exportación (trazabilidad de procedencia).
/// </summary>
public class Campo
{
    public Guid Id { get; set; }

    /// <summary>Finca / lote geográfico (ej. "Marisol").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Establecimiento (ej. "Santa Ana").</summary>
    public string? Establecimiento { get; set; }

    /// <summary>Pivote de riego (ej. "B"), si aplica.</summary>
    public string? Pivote { get; set; }

    /// <summary>Cuadrante / sector dentro del pivote (ej. "6"), si aplica.</summary>
    public string? Cuadrante { get; set; }

    public ICollection<Lote> Lotes { get; set; } = [];
}
