namespace Papasur.Domain.Documentos;

/// <summary>
/// Plantilla / requisito documental: define QUÉ campos exige un documento para un organismo y país
/// concretos. Es DATO configurable (no código): se carga y actualiza sin redeploy. Acá viven los
/// "requisitos documentales" que provee Papasud (proformas, formularios de organismos de control).
/// </summary>
public class PlantillaDocumento
{
    public Guid Id { get; set; }

    /// <summary>
    /// Código estable del tipo de documento (contrato §4): proforma_invoice, packing_list...
    /// Es lo que el front usa como identidad; el Id es interno.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

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

    /// <summary>
    /// Sobre qué se genera: un lote suelto (copiloto de trazabilidad) o un formulario de
    /// exportación completo (contrato §5). Ver <see cref="AmbitosPlantilla"/>.
    /// </summary>
    public string Ambito { get; set; } = AmbitosPlantilla.Lote;

    public DateTime CreatedAt { get; set; }

    public ICollection<CampoPlantilla> Campos { get; set; } = [];
}

/// <summary>Tipos de documento de exportación conocidos (string estable, se guarda tal cual).</summary>
public static class TiposDocumento
{
    public const string Proforma = "proforma";

    public const string CertificadoFitosanitario = "certificado_fitosanitario";

    public const string DeclaracionExportacion = "declaracion_exportacion";

    public const string PackingList = "packing_list";

    public const string CertificadoOrigen = "certificado_origen";

    public const string AnalisisSemilla = "analisis_semilla";

    public const string Rotulos = "rotulos";

    public static readonly string[] All =
    [
        Proforma, CertificadoFitosanitario, DeclaracionExportacion,
        PackingList, CertificadoOrigen, AnalisisSemilla, Rotulos,
    ];

    public static bool Exists(string tipo) => All.Contains(tipo);
}

/// <summary>Ámbito de una plantilla: qué entidad alimenta la inferencia.</summary>
public static class AmbitosPlantilla
{
    /// <summary>Se genera sobre un lote y un movimiento (vertical del copiloto).</summary>
    public const string Lote = "lote";

    /// <summary>Se genera sobre un formulario de exportación completo (contrato §5).</summary>
    public const string Formulario = "formulario";

    public static readonly string[] All = [Lote, Formulario];

    public static bool Exists(string ambito) => All.Contains(ambito);
}

/// <summary>
/// Códigos de tipo de documento del contrato §4. Son los seis que el front conoce y muestra;
/// cambiarlos es cambiar el contrato, no un detalle interno.
/// </summary>
public static class CodigosDocumento
{
    public const string ProformaInvoice = "proforma_invoice";

    public const string PackingList = "packing_list";

    public const string PhytosanitaryRequest = "phytosanitary_request";

    public const string OriginCertificate = "origin_certificate";

    public const string SeedAnalysisCertificate = "seed_analysis_certificate";

    public const string LotLabels = "lot_labels";

    public static readonly string[] All =
    [
        ProformaInvoice, PackingList, PhytosanitaryRequest,
        OriginCertificate, SeedAnalysisCertificate, LotLabels,
    ];

    public static bool Exists(string codigo) => All.Contains(codigo);
}
