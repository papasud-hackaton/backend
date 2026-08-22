using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Customers.Ports;

/// <summary>Puerto de clientes / importadores (contrato §3).</summary>
public interface ICustomerRepository
{
    /// <summary>Búsqueda por nombre. Sin paginar: el contrato devuelve el array completo.</summary>
    Task<IReadOnlyList<Cliente>> ListAsync(string? search, CancellationToken cancellationToken);

    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Cliente customer, CancellationToken cancellationToken);
}
