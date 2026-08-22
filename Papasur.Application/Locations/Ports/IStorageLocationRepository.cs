using Papasur.Domain.Inventory;

namespace Papasur.Application.Locations.Ports;

/// <summary>Puerto del catálogo de ubicaciones. Son cuatro y son de sólo lectura.</summary>
public interface IStorageLocationRepository
{
    Task<IReadOnlyList<StorageLocation>> ListAsync(CancellationToken cancellationToken);
}
