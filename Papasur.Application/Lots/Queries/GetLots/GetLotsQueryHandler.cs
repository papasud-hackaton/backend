using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Lots.Ports;

namespace Papasur.Application.Lots.Queries.GetLots;

public sealed class GetLotsQueryHandler(ILotProjectionRepository lots)
    : IQueryHandler<GetLotsQuery, Result<PagedResult<SeedLotDto>>>
{
    public async Task<Result<PagedResult<SeedLotDto>>> Handle(GetLotsQuery query, CancellationToken cancellationToken)
        => Result.Success(await lots.ListAsync(query.Page, query.Filter, cancellationToken));
}
