using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Statuses.Queries.GetStatuses;

public sealed record GetStatusesQuery(PageRequest Page) : IQuery<PagedResult<StatusDto>>;
