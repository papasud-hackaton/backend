using Papasur.Domain.Inventory;

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

    /// <summary>Campaña / año de cosecha. Si falta, se deriva del movimiento más antiguo.</summary>
    public int? Campania { get; set; }

    /// <summary>Dónde está guardado hoy (3 cámaras + galpón).</summary>
    public Guid? StorageLocationId { get; set; }

    public StorageLocation? StorageLocation { get; set; }

    /// <summary>Posición dentro de la ubicación (pasillo, estiba), si se registró.</summary>
    public string? Posicion { get; set; }

    // --- Calidad. Hoy no viene en la planilla; queda como dato para cuando Papasud lo entregue.
    // Mientras esté vacío el sistema lo advierte, que es justamente lo que tiene que hacer.

    /// <summary>Poder germinativo en porcentaje.</summary>
    public decimal? PoderGerminativo { get; set; }

    /// <summary>Pureza en porcentaje.</summary>
    public decimal? Pureza { get; set; }

    public decimal? Humedad { get; set; }

    public string? Tratamiento { get; set; }

    /// <summary>Número de inscripción en el INASE. Sin esto no se puede certificar.</summary>
    public string? RegistroInase { get; set; }

    /// <summary>Bloqueado para despacho. Es una advertencia BLOQUEANTE al armar un envío.</summary>
    public bool EnCuarentena { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
