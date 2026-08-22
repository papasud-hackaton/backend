using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Items.Queries.ObtenerItems;

public sealed record ObtenerItemsQuery(PageRequest Page) : IQuery<PagedResult<ItemDto>>;
