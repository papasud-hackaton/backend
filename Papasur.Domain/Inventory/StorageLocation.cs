using Papasur.Domain.Trazabilidad;

namespace Papasur.Domain.Inventory;

/// <summary>Cámara de frío o galpón donde se almacena un lote.</summary>
public class StorageLocation
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>cold_room | warehouse (ver LocationTypes).</summary>
    public string Type { get; set; } = LocationTypes.ColdRoom;

    public decimal? TemperatureC { get; set; }

    /// <summary>Lotes almacenados acá (inverso de Lote.StorageLocation).</summary>
    public ICollection<Lote> Lotes { get; set; } = [];
}

public static class LocationTypes
{
    public const string ColdRoom = "cold_room";

    public const string Warehouse = "warehouse";

    public static readonly string[] All = [ColdRoom, Warehouse];
}
