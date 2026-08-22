using Microsoft.EntityFrameworkCore;
using Papasur.Application.Metrics.Ports;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Metrics;

/// <summary>Métricas de usuarios: totales, activos y desagregado por rol.</summary>
public sealed class UserMetricProvider(AppDbContext db) : IMetricProvider
{
    public string Key => "users";

    public async Task<IReadOnlyList<MetricValue>> GetAsync(
        MetricFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (filter.From is { } from)
        {
            query = query.Where(u => u.CreatedAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(u => u.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var active = await query.CountAsync(u => u.IsActive, cancellationToken);

        var byRole = await query
            .GroupBy(u => u.Role.Name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var metrics = new List<MetricValue>
        {
            new("users.total", "Usuarios", total),
            new("users.active", "Usuarios activos", active),
            new("users.inactive", "Usuarios inactivos", total - active),
        };

        metrics.AddRange(byRole.Select(r =>
            new MetricValue("users.by_role", $"Usuarios con rol {r.Role}", r.Count, r.Role)));

        return metrics;
    }
}
