using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Audit.Ports;
using Papasur.Domain.Audit;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Audit;

public class EfAuditRepository(AppDbContext db) : IAuditRepository
{
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditEntry>> ListAsync(
        PageRequest page,
        AuditFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.AuditEntries
            .AsNoTracking()
            .Include(a => a.User)
            .AsQueryable();

        if (filter.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            query = query.Where(a => a.EntityId == filter.EntityId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(a => a.OccurredAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(a => a.OccurredAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);

        var entries = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenBy(a => a.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntry>(entries, page.Page, page.PageSize, total);
    }
}
