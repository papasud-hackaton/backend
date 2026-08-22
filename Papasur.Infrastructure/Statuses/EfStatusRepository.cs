using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Statuses.Ports;
using Papasur.Domain.Statuses;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Statuses;

public class EfStatusRepository(AppDbContext db) : IStatusRepository
{
    public async Task<PagedResult<Status>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Statuses.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var statuses = await query
            .OrderBy(s => s.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Status>(statuses, page.Page, page.PageSize, total);
    }

    public async Task<Status?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => await db.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

    public Task<bool> ExistsAsync(int statusId, CancellationToken cancellationToken)
        => db.Statuses.AnyAsync(s => s.Id == statusId, cancellationToken);
}
