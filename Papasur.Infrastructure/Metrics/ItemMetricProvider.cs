using Microsoft.EntityFrameworkCore;
using Papasur.Application.Metrics.Ports;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Metrics;

/// <summary>
/// Métricas de items (feature de ejemplo). Sirve de plantilla: copiar esta clase,
/// cambiar la tabla y registrarla en DependencyInjection para tener métricas nuevas.
/// </summary>
public sealed class ItemMetricProvider(AppDbContext db) : IMetricProvider
{
    public string Key => "items";

    public async Task<IReadOnlyList<MetricValue>> GetAsync(
        MetricFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Items.AsNoTracking().AsQueryable();

        if (filter.From is { } from)
        {
            query = query.Where(i => i.FechaRegistro >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(i => i.FechaRegistro <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var sum = total == 0 ? 0m : await query.SumAsync(i => i.Valor, cancellationToken);

        return
        [
            new MetricValue("items.total", "Items", total),
            new MetricValue("items.value_sum", "Suma de valores", sum),
            new MetricValue("items.value_avg", "Valor promedio", total == 0 ? 0m : sum / total),
        ];
    }
}
