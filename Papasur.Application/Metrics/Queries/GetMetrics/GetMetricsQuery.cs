using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Metrics.Ports;

namespace Papasur.Application.Metrics.Queries.GetMetrics;

/// <summary>
/// Métricas básicas. Sources permite pedir sólo algunos proveedores (por ejemplo ["users"]);
/// vacío o null devuelve todos.
/// </summary>
public sealed record GetMetricsQuery(PageRequest Page, MetricFilter Filter, string[]? Sources = null)
    : IQuery<Result<PagedResult<MetricDto>>>;
