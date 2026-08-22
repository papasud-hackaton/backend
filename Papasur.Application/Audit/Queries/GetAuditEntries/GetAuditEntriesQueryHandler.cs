using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;

namespace Papasur.Application.Audit.Queries.GetAuditEntries;

public sealed class GetAuditEntriesQueryHandler(IAuditRepository audit)
    : IQueryHandler<GetAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>>
{
    public async Task<Result<PagedResult<AuditEntryDto>>> Handle(
        GetAuditEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Filter.From is { } from && query.Filter.To is { } to && from > to)
        {
            return Result.Failure<PagedResult<AuditEntryDto>>(new Error(
                "Audit.InvalidDateRange",
                "La fecha 'desde' no puede ser posterior a la fecha 'hasta'."));
        }

        var page = await audit.ListAsync(query.Page, query.Filter, cancellationToken);

        return Result.Success(page.Map(AuditMapping.ToDto));
    }
}

/// <summary>Proyección única de la entrada de auditoría.</summary>
public static class AuditMapping
{
    public static AuditEntryDto ToDto(Domain.Audit.AuditEntry e) => new(
        e.Id,
        e.UserId,
        e.ActorName,
        e.ActorRole,
        e.Action,
        e.EntityType,
        e.EntityId,
        e.Changes,
        e.Detail,
        e.IpAddress,
        e.OccurredAt);
}
