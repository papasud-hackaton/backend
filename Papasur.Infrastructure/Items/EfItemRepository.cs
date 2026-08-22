using Papasur.Application.Items.Ports;
using Papasur.Domain.Items;
using Papasur.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Papasur.Infrastructure.Items;

/// <summary>
/// Implementación EF del puerto IItemRepository (patrón Ef*Repository).
/// </summary>
public class EfItemRepository(AppDbContext db) : IItemRepository
{
    public async Task AddAsync(Item item, CancellationToken cancellationToken)
    {
        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Items
            .AsNoTracking()
            .OrderByDescending(i => i.FechaRegistro)
            .ToListAsync(cancellationToken);
    }
}
