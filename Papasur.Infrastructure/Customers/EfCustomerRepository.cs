using Microsoft.EntityFrameworkCore;
using Papasur.Application.Customers.Ports;
using Papasur.Domain.Trazabilidad;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Customers;

public class EfCustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<IReadOnlyList<Cliente>> ListAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Clientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => EF.Functions.ILike(c.Nombre, $"%{search}%"));
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
    }

    public async Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken)
        => db.Clientes.AnyAsync(c => c.Nombre.ToLower() == name.ToLower(), cancellationToken);

    public async Task AddAsync(Cliente customer, CancellationToken cancellationToken)
    {
        db.Clientes.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
    }
}
