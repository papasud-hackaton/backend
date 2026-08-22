namespace Papasur.Application.Metrics.Queries.GetMetrics;

public sealed record MetricDto(string Source, string Key, string Label, decimal Value, string? Group);
