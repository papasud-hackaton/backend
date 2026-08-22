using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Items.Ports;

namespace Papasur.Application.Items.Queries.ObtenerItems;

public sealed class ObtenerItemsQueryHandler(IItemRepository repository)
    : IQueryHandler<ObtenerItemsQuery, PagedResult<ItemDto>>
{
    public async Task<PagedResult<ItemDto>> Handle(
        ObtenerItemsQuery query,
        CancellationToken cancellationToken)
    {
        var page = await repository.ListAsync(query.Page, cancellationToken);

        return page.Map(i => new ItemDto(i.Id, i.Nombre, i.Valor, i.FechaRegistro));
    }
}
