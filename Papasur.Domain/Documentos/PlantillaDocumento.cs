namespace Papasur.Domain.Documentos;

/// <summary>
/// Plantilla / requisito documental: define QUÉ campos exige un documento para un organismo y país
/// concretos. Es DATO configurable (no código): se carga y actualiza sin redeploy. Acá viven los
/// "requisitos documentales" que provee Papasud (proformas, formularios de organismos de control).
/// </summary>
public class PlantillaDocumento
{
    public Guid Id { get; set; }

    /// <summary>Nombre visible (ej. "Proforma exportación Brasil").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Tipo de documento (ver <see cref="TiposDocumento"/>): proforma, certificado_fitosanitario...</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Organismo de control que lo exige (ej. "SENASA"), si aplica.</summary>
    public string? Organismo { get; set; }

    /// <summary>País de destino al que aplica, si aplica.</summary>
    public string? PaisDestino { get; set; }

    /// <summary>Versión de la plantilla: se copia en cada documento para saber con qué versión se generó.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Sólo las plantillas activas se ofrecen para generar documentos.</summary>
    public bool Activa { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<CampoPlantilla> Campos { get; set; } = [];
}

/// <summary>Tipos de documento de exportación conocidos (string estable, se guarda tal cual).</summary>
public static class TiposDocumento
{
    public const string Proforma = "proforma";

    public const string CertificadoFitosanitario = "certificado_fitosanitario";

    public const string DeclaracionExportacion = "declaracion_exportacion";

    public static readonly string[] All = [Proforma, CertificadoFitosanitario, DeclaracionExportacion];

    public static bool Exists(string tipo) => All.Contains(tipo);
}
