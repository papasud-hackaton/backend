using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Lots.Ports;
using Papasur.Application.Lots.Queries.GetLots;

namespace Papasur.Application.Lots.Queries.GetLotById;

public sealed class GetLotByIdQueryHandler(ILotProjectionRepository lots)
    : IQueryHandler<GetLotByIdQuery, Result<SeedLotDto>>
{
    public async Task<Result<SeedLotDto>> Handle(GetLotByIdQuery query, CancellationToken cancellationToken)
    {
        var lot = await lots.GetByIdAsync(query.Id, cancellationToken);

        return lot is null
            ? Result.Failure<SeedLotDto>(new Error("Lot.NotFound", "Lote no encontrado."))
            : Result.Success(lot);
    }
}
