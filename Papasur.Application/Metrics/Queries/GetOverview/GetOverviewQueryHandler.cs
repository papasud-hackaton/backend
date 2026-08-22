using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Customers;
using Papasur.Application.ExportForms;
using Papasur.Application.ExportForms.Ports;
using Papasur.Domain.ExportForms;
using Papasur.Domain.Users;

namespace Papasur.Application.Metrics.Queries.GetOverview;

/// <summary>
/// Métricas del tablero — portadas del handler del mock (contrato §7), incluyendo las tres cosas
/// que allá faltaban: la serie de 12 semanas, la actividad por agente y que from/to se respeten
/// de verdad.
/// </summary>
public sealed class GetOverviewQueryHandler(IExportFormRepository forms, FormAssembler assembler)
    : IQueryHandler<GetOverviewQuery, Result<MetricsOverviewResult>>
{
    private const int Weeks = 12;

    private const string TeamScope = "team";

    /// <summary>Roles que pueden ver la foto del equipo (capacidad metrics.viewTeam).</summary>
    private static readonly string[] TeamRoles = [RoleNames.Supervisor, RoleNames.Admin];

    public async Task<Result<MetricsOverviewResult>> Handle(GetOverviewQuery query, CancellationToken cancellationToken)
    {
        var wantsTeam = string.Equals(query.Scope, TeamScope, StringComparison.OrdinalIgnoreCase);
        var scopeIsTeam = wantsTeam && TeamRoles.Contains(query.Actor.Role);

        var all = await forms.ListAllForMetricsAsync(
            scopeIsTeam ? null : query.Actor.Id,
            query.From,
            query.To,
            cancellationToken);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var counts = new AgentMetricsDto(
            all.Count(f => f.Status == FormStatuses.Draft),
            all.Count(f => f.Status == FormStatuses.Submitted),
            all.Count(f => f.Status == FormStatuses.Approved),
            all.Count(f => f.CreatedAt >= startOfMonth));

        if (!scopeIsTeam)
        {
            return Result.Success(new MetricsOverviewResult(counts, null));
        }

        var totals = all.ToDictionary(f => f.Id, f => FormCalculations.ComputeTotals(f.Items));

        var reviewed = all.Where(f => f.SubmittedAt is not null && f.ReviewedAt is not null).ToList();

        var avgReviewHours = reviewed.Count == 0
            ? 0m
            : Math.Round(
                (decimal)reviewed.Average(f => Math.Abs((f.ReviewedAt!.Value - f.SubmittedAt!.Value).TotalHours)),
                1,
                MidpointRounding.AwayFromZero);

        var decided = all.Count(f =>
            f.Status is FormStatuses.Approved or FormStatuses.Issued or FormStatuses.ChangesRequested);

        var changesRate = decided == 0
            ? 0m
            : Math.Round(all.Count(f => f.Status == FormStatuses.ChangesRequested) / (decimal)decided, 4);

        var warningCounts = await assembler.ToSummariesAsync(all, cancellationToken);

        var team = new TeamMetricsDto(
            counts.DraftCount,
            counts.SubmittedCount,
            counts.ApprovedCount,
            counts.FormsThisMonth,
            [.. FormStatuses.All.Select(s => new StatusCountDto(s, all.Count(f => f.Status == s)))],
            FormsOverTime(all, totals, now),
            AgentActivity(all, totals),
            all.Where(f => f.Status == FormStatuses.Issued).Sum(f => totals[f.Id].TotalKg),
            avgReviewHours,
            changesRate,
            warningCounts.Sum(s => s.WarningCount),
            TopDestinations(all, totals),
            TopVarieties(all, totals));

        return Result.Success(new MetricsOverviewResult(counts, team));
    }

    /// <summary>Serie de las últimas 12 semanas, con los huecos en cero.</summary>
    private static List<WeekPointDto> FormsOverTime(
        IReadOnlyList<ExportForm> forms,
        Dictionary<Guid, ExportFormTotals> totals,
        DateTime now)
    {
        var buckets = new List<WeekPointDto>(Weeks);

        for (var i = Weeks - 1; i >= 0; i--)
        {
            var start = now.AddDays(-7 * (i + 1));
            var end = now.AddDays(-7 * i);
            var inWeek = forms.Where(f => f.CreatedAt >= start && f.CreatedAt < end).ToList();

            buckets.Add(new WeekPointDto(
                start.ToString("yyyy-MM-dd"),
                inWeek.Count,
                inWeek.Sum(f => totals[f.Id].TotalKg)));
        }

        return buckets;
    }

    private static List<AgentActivityDto> AgentActivity(
        IReadOnlyList<ExportForm> forms,
        Dictionary<Guid, ExportFormTotals> totals)
        => [.. forms
            .GroupBy(f => f.CreatedByUserId)
            .Select(g => new AgentActivityDto(
                g.Key,
                g.Select(f => f.CreatedByUser).FirstOrDefault() is { } user
                    ? $"{user.FirstName} {user.LastName}".Trim()
                    : "—",
                g.Count(),
                g.Sum(f => totals[f.Id].TotalKg)))
            .OrderByDescending(a => a.Count)
            .Take(8)];

    private static List<DestinationVolumeDto> TopDestinations(
        IReadOnlyList<ExportForm> forms,
        Dictionary<Guid, ExportFormTotals> totals)
        => [.. forms
            .GroupBy(f => f.Customer?.Pais ?? Countries.NameOf(f.DestinationCountryCode))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new DestinationVolumeDto(g.Key, g.Sum(f => totals[f.Id].TotalKg)))
            .OrderByDescending(d => d.TotalKg)
            .Take(5)];

    private static List<VarietyVolumeDto> TopVarieties(
        IReadOnlyList<ExportForm> forms,
        Dictionary<Guid, ExportFormTotals> totals)
    {
        var byVariety = new Dictionary<string, decimal>();

        foreach (var form in forms)
        {
            // Una variedad suma el total del envío UNA vez, aunque aparezca en varias líneas.
            foreach (var variety in form.Items.Select(i => i.Traceability.Variety).Distinct())
            {
                if (string.IsNullOrWhiteSpace(variety))
                {
                    continue;
                }

                byVariety[variety] = byVariety.GetValueOrDefault(variety) + totals[form.Id].TotalKg;
            }
        }

        return [.. byVariety
            .Select(kv => new VarietyVolumeDto(kv.Key, kv.Value))
            .OrderByDescending(v => v.TotalKg)
            .Take(5)];
    }
}
