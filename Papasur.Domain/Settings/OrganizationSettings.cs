namespace Papasur.Domain.Settings;

/// <summary>
/// Datos del exportador que van en todos los documentos (razón social, CUIT, domicilio...).
/// Fila única. El front lo trata como un mapa clave/valor, así que se guarda como JSON:
/// agregar un campo no necesita migración.
/// </summary>
public class OrganizationSettings
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; } = SingletonId;

    /// <summary>Mapa clave/valor serializado.</summary>
    public string ValuesJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }
}
