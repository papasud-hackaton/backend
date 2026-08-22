namespace Papasur.Domain.Items;

/// <summary>
/// Entidad de ejemplo del dominio de estadísticas. Reemplazar/extender con el modelo real.
/// </summary>
public class Item
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime CreatedAt { get; set; }
}
