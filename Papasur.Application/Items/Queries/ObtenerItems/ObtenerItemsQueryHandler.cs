using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Items.Ports;

namespace Papasur.Application.Items.Queries.ObtenerItems;

public sealed class ObtenerItemsQueryHandler(IItemRepository repository)
    : IQueryHandler<ObtenerItemsQuery, IReadOnlyList<ItemDto>>
{
    public async Task<IReadOnlyList<ItemDto>> Handle(
        ObtenerItemsQuery query,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListAsync(cancellationToken);

        return items
            .Select(i => new ItemDto(i.Id, i.Nombre, i.Valor, i.FechaRegistro))
            .ToList();
    }
}
