using Papasur.Application.Abstractions;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Ports;

/// <summary>
/// Puerto de persistencia de usuarios. Implementado en Infrastructure (EfUserRepository).
/// </summary>
public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>Trae el usuario con su Role cargado (para armar los claims del JWT).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmployeeIdExistsAsync(string employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Listado paginado. search busca en nombre, apellido, correo y legajo (contrato §2).
    /// </summary>
    Task<PagedResult<User>> ListAsync(
        PageRequest page,
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);
}
