using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Statuses.Ports;

namespace Papasur.Application.Statuses.Queries.GetStatuses;

public sealed class GetStatusesQueryHandler(IStatusRepository statuses)
    : IQueryHandler<GetStatusesQuery, PagedResult<StatusDto>>
{
    public async Task<PagedResult<StatusDto>> Handle(GetStatusesQuery query, CancellationToken cancellationToken)
    {
        var page = await statuses.ListAsync(query.Page, cancellationToken);

        return page.Map(s => new StatusDto(s.Id, s.Code, s.Name));
    }
}
