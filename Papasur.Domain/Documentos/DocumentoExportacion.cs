using Papasur.Domain.Statuses;
using Papasur.Domain.Trazabilidad;
using Papasur.Domain.Users;

namespace Papasur.Domain.Documentos;

/// <summary>
/// Documento de exportación generado para un lote a partir de una plantilla. Es una PROYECCIÓN sobre
/// la trazabilidad: guarda cada valor con su origen (inferido / manual / dictado) para poder auditar
/// qué puso el sistema y qué confirmó una persona. Nada se da por definitivo sin una confirmación
/// humana explícita (<see cref="ConfirmedAt"/>).
/// </summary>
public class DocumentoExportacion
{
    public Guid Id { get; set; }

    public Guid LoteId { get; set; }

    public Lote Lote { get; set; } = null!;

    /// <summary>Movimiento / despacho concreto sobre el que se genera (aporta remito, kilos, DTV), si aplica.</summary>
    public Guid? MovimientoId { get; set; }

    public Movimiento? Movimiento { get; set; }

    public Guid PlantillaDocumentoId { get; set; }

    public PlantillaDocumento PlantillaDocumento { get; set; } = null!;

    /// <summary>Versión de la plantilla usada (se copia al generar: la plantilla puede cambiar después).</summary>
    public int VersionPlantilla { get; set; }

    /// <summary>Estado del ciclo de vida (FK al catálogo Status: en proceso, finalizado, cancelado).</summary>
    public int StatusId { get; set; }

    public Status Status { get; set; } = null!;

    /// <summary>Usuario que lo generó / opera.</summary>
    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Momento de la confirmación humana explícita (null mientras es borrador).</summary>
    public DateTime? ConfirmedAt { get; set; }

    public ICollection<ValorCampo> Valores { get; set; } = [];
}
