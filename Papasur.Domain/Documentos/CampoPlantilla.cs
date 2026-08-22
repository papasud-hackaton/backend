namespace Papasur.Domain.Documentos;

/// <summary>
/// Campo requerido por una plantilla. La <see cref="ReglaMapeo"/> indica de dónde sale su valor por
/// inferencia (ej. "lote.variedad", "movimiento.kilogramos", "movimiento.dtv"): es lo que el motor de
/// inferencia usa para pre-completar cruzando la trazabilidad del lote con el requisito documental.
/// </summary>
public class CampoPlantilla
{
    public Guid Id { get; set; }

    public Guid PlantillaDocumentoId { get; set; }

    public PlantillaDocumento PlantillaDocumento { get; set; } = null!;

    /// <summary>Clave técnica del campo (snake_case, estable): "peso_neto", "variedad", "dtv".</summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Etiqueta visible para el usuario.</summary>
    public string Etiqueta { get; set; } = string.Empty;

    /// <summary>Tipo de dato esperado (ver <see cref="TiposDato"/>): texto, numero, fecha, booleano.</summary>
    public string TipoDato { get; set; } = TiposDato.Texto;

    public bool Obligatorio { get; set; }

    /// <summary>
    /// Regla de mapeo: de dónde sale el valor por inferencia. Para las plantillas de ámbito lote son
    /// rutas de trazabilidad ("movimiento.dtv"); para las de ámbito formulario son las rutas del
    /// contrato §4 ("items[].traceability.variety", "customer.name"). null = sólo manual/dictado.
    /// </summary>
    public string? ReglaMapeo { get; set; }

    /// <summary>De qué entidad se autocompleta (contrato §4): lot, customer, form, organization, manual.</summary>
    public string Origen { get; set; } = OrigenesCampo.Manual;

    /// <summary>Ayuda para quien complete el campo a mano.</summary>
    public string? Ayuda { get; set; }

    /// <summary>Orden de aparición en el formulario de revisión.</summary>
    public int Orden { get; set; }
}

/// <summary>Origen declarado de un campo (contrato §4, FieldSource del front).</summary>
public static class OrigenesCampo
{
    public const string Lot = "lot";

    public const string Customer = "customer";

    public const string Form = "form";

    public const string Organization = "organization";

    public const string Manual = "manual";

    public static readonly string[] All = [Lot, Customer, Form, Organization, Manual];

    public static bool Exists(string origen) => All.Contains(origen);
}

/// <summary>Tipos de dato soportados por un campo (string estable).</summary>
public static class TiposDato
{
    public const string Texto = "texto";

    public const string Numero = "numero";

    public const string Fecha = "fecha";

    public const string Booleano = "booleano";

    public static readonly string[] All = [Texto, Numero, Fecha, Booleano];

    public static bool Exists(string tipo) => All.Contains(tipo);
}
