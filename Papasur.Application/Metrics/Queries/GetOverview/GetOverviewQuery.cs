using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Metrics.Queries.GetOverview;

public sealed record GetOverviewQuery(string? Scope, DateTime? From, DateTime? To, Actor Actor)
    : IQuery<Result<MetricsOverviewResult>>;
