using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Trazabilidad.Ports;

namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotes;

public sealed class ObtenerLotesQueryHandler(ILoteRepository lotes)
    : IQueryHandler<ObtenerLotesQuery, PagedResult<LoteDto>>
{
    public async Task<PagedResult<LoteDto>> Handle(ObtenerLotesQuery query, CancellationToken cancellationToken)
    {
        var page = await lotes.ListAsync(
            query.Page,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.VariedadId,
            cancellationToken);

        return page.Map(l => new LoteDto(
            l.Id,
            l.Codigo,
            l.VariedadId,
            l.Variedad?.Nombre ?? string.Empty,
            l.CampoId,
            l.Campo?.Nombre,
            l.Categoria,
            l.SuperficieHa,
            l.Movimientos.Count,
            l.CreatedAt));
    }
}
