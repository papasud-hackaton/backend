using Papasur.Domain.Items;

namespace Papasur.Application.Items.Ports;

/// <summary>
/// Puerto de persistencia de items. Implementado en Infrastructure (EfItemRepository).
/// </summary>
public interface IItemRepository
{
    Task AddAsync(Item item, CancellationToken cancellationToken);

    Task<IReadOnlyList<Item>> ListAsync(CancellationToken cancellationToken);
}
