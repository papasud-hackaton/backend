using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Lots.Queries.GetLots;

namespace Papasur.Application.Lots.Queries.GetLotById;

public sealed record GetLotByIdQuery(Guid Id) : IQuery<Result<SeedLotDto>>;
