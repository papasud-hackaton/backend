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
        var filter = query.Filter;

        if (filter.From is { } from && filter.To is { } to && from > to)
        {
            return Result.Failure<PagedResult<AuditEntryDto>>(new Error(
                "Audit.InvalidDateRange",
                "La fecha 'desde' no puede ser posterior a la fecha 'hasta'."));
        }

        var page = await audit.ListAsync(query.Page, filter, cancellationToken);

        return Result.Success(page.Map(e => new AuditEntryDto(
            e.Id,
            e.UserId,
            e.User?.Name ?? string.Empty,
            e.User?.Email ?? string.Empty,
            e.User?.EmployeeNumber ?? string.Empty,
            e.Action,
            e.EntityType,
            e.EntityId,
            e.Detail,
            e.IpAddress,
            e.OccurredAt)));
    }
}
