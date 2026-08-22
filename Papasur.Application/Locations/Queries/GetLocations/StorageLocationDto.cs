namespace Papasur.Application.Locations.Queries.GetLocations;

/// <summary>Ubicación de stock en el shape del contrato §3.</summary>
public sealed record StorageLocationDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    decimal? TemperatureC);
