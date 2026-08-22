using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotes;

public sealed record ObtenerLotesQuery(PageRequest Page, string? Search, Guid? VariedadId)
    : IQuery<PagedResult<LoteDto>>;
