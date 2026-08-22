namespace Papasur.Application.Metrics.Ports;

/// <summary>Ventana de tiempo (UTC) sobre la que se calculan las métricas. Todo opcional.</summary>
public sealed record MetricFilter(DateTime? From = null, DateTime? To = null);

/// <summary>
/// Un valor de métrica. Group permite desagregar la misma métrica en varias filas
/// (por ejemplo "users.by_role" agrupado por admin / supervisor / agente).
/// </summary>
public sealed record MetricValue(string Key, string Label, decimal Value, string? Group = null);

/// <summary>
/// Fuente de métricas. Cada proveedor aporta las métricas de SU área y no conoce a los demás.
/// Para agregar métricas nuevas: implementar esta interfaz en Infrastructure y registrarla
/// en DependencyInjection — no hay que tocar el handler ni el controller.
/// </summary>
public interface IMetricProvider
{
    /// <summary>Prefijo/namespace de las métricas de este proveedor (por ejemplo "users").</summary>
    string Key { get; }

    Task<IReadOnlyList<MetricValue>> GetAsync(MetricFilter filter, CancellationToken cancellationToken);
}
