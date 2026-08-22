using Papasur.Application.Abstractions;
using Papasur.Domain.Statuses;

namespace Papasur.Application.Statuses.Ports;

/// <summary>
/// Puerto de lectura del catálogo de estados. Implementado en Infrastructure (EfStatusRepository).
/// </summary>
public interface IStatusRepository
{
    Task<PagedResult<Status>> ListAsync(PageRequest page, CancellationToken cancellationToken);

    Task<Status?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int statusId, CancellationToken cancellationToken);
}
