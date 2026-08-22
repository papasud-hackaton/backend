using Papasur.Application.Abstractions;
using Papasur.Domain.Users;

namespace Papasur.Application.Roles.Ports;

/// <summary>
/// Puerto de lectura del catálogo de roles (tabla fija sembrada por migración).
/// </summary>
public interface IRoleRepository
{
    Task<PagedResult<Role>> ListAsync(PageRequest page, CancellationToken cancellationToken);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int roleId, CancellationToken cancellationToken);
}
