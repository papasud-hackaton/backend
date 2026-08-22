namespace Papasur.Domain.Documentos;

/// <summary>
/// Valor de un campo dentro de un documento generado. Guarda el origen y si fue confirmado, para
/// poder responder siempre "¿este dato lo infirió el sistema o lo cargó/corrigió un humano?".
/// </summary>
public class ValorCampo
{
    public Guid Id { get; set; }

    public Guid DocumentoExportacionId { get; set; }

    public DocumentoExportacion DocumentoExportacion { get; set; } = null!;

    public Guid CampoPlantillaId { get; set; }

    public CampoPlantilla CampoPlantilla { get; set; } = null!;

    /// <summary>Valor final del campo (null si quedó vacío / pendiente de completar).</summary>
    public string? Valor { get; set; }

    public OrigenValor Origen { get; set; }

    /// <summary>Confirmado explícitamente por una persona.</summary>
    public bool Confirmado { get; set; }

    /// <summary>Traza de dónde se infirió el valor (ej. "movimiento.dtv"), cuando Origen = Inferido.</summary>
    public string? InferidoDesde { get; set; }
}

/// <summary>
/// De dónde salió el valor de un campo: clave para la trazabilidad "inferido vs. humano" que exige
/// la consigna (la IA sólo sugiere; el humano confirma).
/// </summary>
public enum OrigenValor
{
    /// <summary>Pre-completado por el sistema cruzando la trazabilidad del lote con el requisito.</summary>
    Inferido = 0,

    /// <summary>Ingresado o corregido a mano por el usuario.</summary>
    Manual = 1,

    /// <summary>Capturado por dictado (voz a texto).</summary>
    Dictado = 2,
}
