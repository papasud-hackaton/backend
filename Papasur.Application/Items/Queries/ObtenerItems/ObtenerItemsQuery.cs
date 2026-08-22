using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Items.Queries.ObtenerItems;

public sealed record ObtenerItemsQuery : IQuery<IReadOnlyList<ItemDto>>;
