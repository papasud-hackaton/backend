namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Variedad de papa semilla (agata, spunta, king russet, memphis, asterix, quintera, sunred...).
/// Dato maestro tomado de la planilla de movimientos (columna "Variedad").
/// </summary>
public class Variedad
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public ICollection<Lote> Lotes { get; set; } = [];
}
