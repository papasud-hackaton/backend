using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Metrics.Ports;

namespace Papasur.Application.Metrics.Queries.GetMetrics;

/// <summary>
/// Recorre TODOS los proveedores registrados (o los pedidos por Sources) y devuelve sus
/// métricas paginadas. Agregar una métrica nueva = registrar un IMetricProvider más.
/// </summary>
public sealed class GetMetricsQueryHandler(IEnumerable<IMetricProvider> providers)
    : IQueryHandler<GetMetricsQuery, Result<PagedResult<MetricDto>>>
{
    public async Task<Result<PagedResult<MetricDto>>> Handle(
        GetMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = query.Filter;

        if (filter.From is { } from && filter.To is { } to && from > to)
        {
            return Result.Failure<PagedResult<MetricDto>>(new Error(
                "Metrics.InvalidDateRange",
                "La fecha 'desde' no puede ser posterior a la fecha 'hasta'."));
        }

        var wanted = query.Sources is { Length: > 0 }
            ? providers.Where(p => query.Sources.Contains(p.Key, StringComparer.OrdinalIgnoreCase)).ToList()
            : providers.ToList();

        if (query.Sources is { Length: > 0 } && wanted.Count == 0)
        {
            return Result.Failure<PagedResult<MetricDto>>(new Error(
                "Metrics.SourceNotFound",
                $"No hay métricas para: {string.Join(", ", query.Sources)}."));
        }

        var metrics = new List<MetricDto>();

        foreach (var provider in wanted.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var values = await provider.GetAsync(filter, cancellationToken);

            metrics.AddRange(values.Select(v => new MetricDto(provider.Key, v.Key, v.Label, v.Value, v.Group)));
        }

        var page = query.Page;

        var items = metrics
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToList();

        return Result.Success(new PagedResult<MetricDto>(items, page.Page, page.PageSize, metrics.Count));
    }
}
