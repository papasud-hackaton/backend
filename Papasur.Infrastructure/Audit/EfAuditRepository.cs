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
        var query = Filtrar(filter);

        var total = await query.CountAsync(cancellationToken);

        var entries = await Ordenar(query)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntry>(entries, page.Page, page.PageSize, total);
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAllAsync(
        AuditFilter filter,
        CancellationToken cancellationToken)
        => await Ordenar(Filtrar(filter)).ToListAsync(cancellationToken);

    private IQueryable<AuditEntry> Filtrar(AuditFilter filter)
    {
        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (filter.ActorId is { } actorId)
        {
            query = query.Where(a => a.UserId == actorId);
        }

        if (filter.Actions is { Count: > 0 } actions)
        {
            query = query.Where(a => actions.Contains(a.Action));
        }

        if (filter.Roles is { Count: > 0 } roles)
        {
            // Filtra por el rol que la persona tenía al momento del hecho, no por el actual.
            query = query.Where(a => roles.Contains(a.ActorRole));
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            query = query.Where(a => a.EntityId == filter.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.ActorName, pattern)
                || EF.Functions.ILike(a.Action, pattern)
                || (a.Detail != null && EF.Functions.ILike(a.Detail, pattern)));
        }

        if (filter.From is { } from)
        {
            query = query.Where(a => a.OccurredAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(a => a.OccurredAt <= to);
        }

        return query;
    }

    private static IQueryable<AuditEntry> Ordenar(IQueryable<AuditEntry> query)
        => query.OrderByDescending(a => a.OccurredAt).ThenBy(a => a.Id);
}
