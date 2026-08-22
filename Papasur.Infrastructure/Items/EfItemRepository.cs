using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Items.Ports;
using Papasur.Domain.Items;
using Papasur.Infrastructure.Persistence;

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

    public async Task<PagedResult<Item>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Items.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.FechaRegistro)
            .ThenBy(i => i.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Item>(items, page.Page, page.PageSize, total);
    }
}
