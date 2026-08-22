namespace Papasur.Application.Metrics.Queries.GetOverview;

/// <summary>Lo que ve un agente sobre lo suyo (contrato §7).</summary>
public record AgentMetricsDto(
    int DraftCount,
    int SubmittedCount,
    int ApprovedCount,
    int FormsThisMonth);

public sealed record StatusCountDto(string Status, int Count);

public sealed record WeekPointDto(string WeekStart, int Count, decimal TotalKg);

public sealed record AgentActivityDto(Guid UserId, string Name, int Count, decimal TotalKg);

public sealed record DestinationVolumeDto(string CountryName, decimal TotalKg);

public sealed record VarietyVolumeDto(string Variety, decimal TotalKg);

/// <summary>Lo anterior más la foto del equipo (contrato §7). Sólo para supervisor y admin.</summary>
public sealed record TeamMetricsDto(
    int DraftCount,
    int SubmittedCount,
    int ApprovedCount,
    int FormsThisMonth,
    IReadOnlyList<StatusCountDto> FormsByStatus,
    IReadOnlyList<WeekPointDto> FormsOverTime,
    IReadOnlyList<AgentActivityDto> AgentActivity,
    decimal ExportedVolumeKg,
    decimal AvgReviewTimeHours,
    decimal ChangesRequestedRate,
    int StockWarningsCount,
    IReadOnlyList<DestinationVolumeDto> TopDestinations,
    IReadOnlyList<VarietyVolumeDto> TopVarieties)
    : AgentMetricsDto(DraftCount, SubmittedCount, ApprovedCount, FormsThisMonth);

/// <summary>
/// Un agente que pide scope=team recibe sus propios datos, no un 403 (contrato §7):
/// degradar es mejor que romper la pantalla.
/// </summary>
public sealed record MetricsOverviewResult(AgentMetricsDto Agent, TeamMetricsDto? Team);
