using Microsoft.EntityFrameworkCore;
using Papasur.Application.Locations.Ports;
using Papasur.Domain.Inventory;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Locations;

public class EfStorageLocationRepository(AppDbContext db) : IStorageLocationRepository
{
    public async Task<IReadOnlyList<StorageLocation>> ListAsync(CancellationToken cancellationToken)
        => await db.StorageLocations.AsNoTracking().OrderBy(l => l.Code).ToListAsync(cancellationToken);
}
