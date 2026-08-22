using Papasur.Domain.Documentos;

namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Movimiento / despacho de un lote: una fila de la planilla de movimientos. El Excel real tiene
/// varias hojas que son etapas de la misma cadena (ingreso a tolvas, campo→frío, envío/retiro de
/// frío, entregas a clientes); <see cref="Tipo"/> distingue la etapa. Concentra los datos operativos
/// que alimentan la mayoría de los campos de una proforma: remito, fecha, kilos, bolsas, transporte,
/// destino, categoría/calibre y el DTV (Documento de Tránsito Vegetal).
/// </summary>
public class Movimiento
{
    public Guid Id { get; set; }

    public Guid LoteId { get; set; }

    public Lote Lote { get; set; } = null!;

    /// <summary>Etapa del movimiento (ver <see cref="TiposMovimiento"/>): a qué hoja pertenece.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Número de remito (columna "Remito"). Puede ser "s/remito".</summary>
    public string NumeroRemito { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    /// <summary>Kilogramos del despacho (columna "Kgs").</summary>
    public decimal Kilogramos { get; set; }

    /// <summary>Cantidad de bolsas (columna "Bolsas"); null cuando va a granel.</summary>
    public int? Bolsas { get; set; }

    /// <summary>Kilo promedio por bolsa (columna "Kg.Prom"), si se registró.</summary>
    public decimal? KgPromedio { get; set; }

    /// <summary>Presentación: granel, bolson, bolsa... (a veces viene en la columna "Bolsas").</summary>
    public string? Presentacion { get; set; }

    /// <summary>Categoría comercial (columna "Categoría"): "solo chasis", "(lamb weston)", etc.</summary>
    public string? Categoria { get; set; }

    /// <summary>Calibre / destino de calidad (columna "Calibre"): "exportacion", "sin chicas", "recibo"...</summary>
    public string? Calibre { get; set; }

    public Guid? TransportistaId { get; set; }

    public Transportista? Transportista { get; set; }

    /// <summary>Cliente / comprador (columnas "Cliente" / "Destino" comercial: parmentier, wemar-mc cain...).</summary>
    public Guid? ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    /// <summary>Comisionista interviniente (columna "Comisionista"), si aplica.</summary>
    public string? Comisionista { get; set; }

    /// <summary>Destino logístico / físico (columna "Destino": dospanca, galpon, planta...).</summary>
    public string? Destino { get; set; }

    /// <summary>Número de DTV (Documento de Tránsito Vegetal). Es el dato regulatorio clave del movimiento.</summary>
    public string? Dtv { get; set; }

    /// <summary>Observaciones libres: tipo/color de bolsa e hilo, "sin tamañar", "afectada", etc.</summary>
    public string? Observaciones { get; set; }

    public ICollection<DocumentoExportacion> Documentos { get; set; } = [];
}

/// <summary>Etapas de movimiento conocidas (una por hoja del Excel de movimientos).</summary>
public static class TiposMovimiento
{
    public const string IngresoTolva = "ingreso_tolva";

    public const string CampoAFrio = "campo_a_frio";

    public const string EnvioFrio = "envio_frio";

    public const string RetiroFrio = "retiro_frio";

    public const string EntregaCliente = "entrega_cliente";

    public static readonly string[] All =
        [IngresoTolva, CampoAFrio, EnvioFrio, RetiroFrio, EntregaCliente];

    public static bool Exists(string tipo) => All.Contains(tipo);
}
