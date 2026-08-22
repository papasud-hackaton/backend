namespace Papasur.Domain.Trazabilidad;

/// <summary>
/// Cliente / destino comercial del despacho (planilla de movimientos, columna "Destino":
/// dospanca, galpon...). En una proforma de exportación cumple el rol de comprador / importador,
/// por eso lleva los datos fiscales y de domicilio que exigen los documentos (contrato §3).
/// </summary>
public class Cliente
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Identificación fiscal en su país (va en la proforma y en el certificado de origen).</summary>
    public string? TaxId { get; set; }

    /// <summary>ISO 3166-1 alfa-2: BR, PY, UY. Es la clave; el nombre del país se deriva.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Nombre del país tal como se imprime. Redundante a propósito: los documentos lo exigen.</summary>
    public string? Pais { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    /// <summary>Valores por defecto que precargan el paso 1 del wizard.</summary>
    public string? DefaultIncoterm { get; set; }

    public string? DefaultPortOfDischarge { get; set; }

    public ICollection<Movimiento> Movimientos { get; set; } = [];
}
