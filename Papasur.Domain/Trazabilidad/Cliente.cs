namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Cliente / destino comercial del despacho (planilla de movimientos, columna "Destino":
/// dospanca, galpon...). En una proforma de exportación cumple el rol de comprador / importador,
/// por eso lleva un país opcional que alimenta los documentos de exportación.
/// </summary>
public class Cliente
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>País de destino (para exportación), si se conoce.</summary>
    public string? Pais { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
