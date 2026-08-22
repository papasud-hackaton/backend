using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Lots.Ports;

namespace Papasur.Application.Lots.Queries.GetLots;

public sealed record GetLotsQuery(PageRequest Page, LotFilter Filter)
    : IQuery<Result<PagedResult<SeedLotDto>>>;
