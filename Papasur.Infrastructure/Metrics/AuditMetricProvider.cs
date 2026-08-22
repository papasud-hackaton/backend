using Microsoft.EntityFrameworkCore;
using Papasur.Application.Metrics.Ports;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Metrics;

/// <summary>Métricas de auditoría: eventos totales, agentes distintos y desagregado por acción.</summary>
public sealed class AuditMetricProvider(AppDbContext db) : IMetricProvider
{
    public string Key => "audit";

    public async Task<IReadOnlyList<MetricValue>> GetAsync(
        MetricFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (filter.From is { } from)
        {
            query = query.Where(a => a.OccurredAt >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(a => a.OccurredAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var agents = await query.Select(a => a.UserId).Distinct().CountAsync(cancellationToken);

        var byAction = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var metrics = new List<MetricValue>
        {
            new("audit.events", "Eventos auditados", total),
            new("audit.agents", "Agentes con actividad", agents),
        };

        metrics.AddRange(byAction.Select(a =>
            new MetricValue("audit.by_action", $"Eventos '{a.Action}'", a.Count, a.Action)));

        return metrics;
    }
}
