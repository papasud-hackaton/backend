using Papasur.Domain.Trazabilidad;

namespace Papasur.Domain.ExportForms;

/// <summary>
/// Línea del formulario: una cantidad de UN lote real. Nunca texto libre.
/// Congela la trazabilidad al agregarse (<see cref="Traceability"/>): si el lote cambia
/// después, el documento ya emitido no miente.
/// </summary>
public class ExportFormItem
{
    public Guid Id { get; set; }

    public Guid ExportFormId { get; set; }

    public ExportForm ExportForm { get; set; } = null!;

    public Guid LotId { get; set; }

    public Lote Lot { get; set; } = null!;

    public decimal QuantityKg { get; set; }

    public string PackagingType { get; set; } = PackagingTypes.Bulk;

    public int PackagesCount { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    /// <summary>Orden de la línea dentro del envío: la paginación necesita un desempate estable.</summary>
    public int Position { get; set; }

    /// <summary>Foto de la trazabilidad al momento de agregar la línea.</summary>
    public TraceabilitySnapshot Traceability { get; set; } = new();
}

/// <summary>
/// Trazabilidad congelada de una línea. Es un tipo de propiedad (columnas de la misma tabla):
/// no tiene identidad propia, vive y muere con la línea.
/// </summary>
public class TraceabilitySnapshot
{
    public string LotCode { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Variety { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int CropYear { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public decimal? GerminationRate { get; set; }

    public decimal? Purity { get; set; }

    public string? InaseRegistration { get; set; }

    public DateTime CapturedAt { get; set; }
}

/// <summary>Presentaciones y su peso por bulto (contrato §5, calculations.ts del front).</summary>
public static class PackagingTypes
{
    public const string BigBag = "big_bag";

    public const string Bag25Kg = "bag_25kg";

    public const string Bag50Kg = "bag_50kg";

    public const string Box = "box";

    /// <summary>A granel: el envío entero es un solo bulto.</summary>
    public const string Bulk = "bulk";

    public static readonly string[] All = [BigBag, Bag25Kg, Bag50Kg, Box, Bulk];

    public static bool Exists(string type) => All.Contains(type);

    /// <summary>Kilos por bulto; null a granel.</summary>
    public static decimal? WeightOf(string type) => type switch
    {
        BigBag => 1000m,
        Bag25Kg => 25m,
        Bag50Kg => 50m,
        Box => 20m,
        _ => null,
    };
}
