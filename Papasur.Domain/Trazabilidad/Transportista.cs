namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Transportista que realiza el despacho (planilla de movimientos, columna "Transporte":
/// serantes-vera, Camilo Gastón, Arenas...).
/// </summary>
public class Transportista
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
