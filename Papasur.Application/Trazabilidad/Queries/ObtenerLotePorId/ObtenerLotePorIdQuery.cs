using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;

public sealed record ObtenerLotePorIdQuery(Guid Id) : IQuery<Result<LoteDetalleDto>>;
