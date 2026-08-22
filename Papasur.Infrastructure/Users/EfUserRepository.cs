using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Users;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Users;

public class EfUserRepository(AppDbContext db) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        => await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmployeeIdExistsAsync(string employeeId, CancellationToken cancellationToken)
        => db.Users.AnyAsync(u => u.EmployeeId == employeeId, cancellationToken);

    public async Task<PagedResult<User>> ListAsync(
        PageRequest page,
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Contrato §2: busca en nombre, apellido, correo y legajo.
            var pattern = $"%{search}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName, pattern)
                || EF.Functions.ILike(u.LastName, pattern)
                || EF.Functions.ILike(u.Email, pattern)
                || EF.Functions.ILike(u.EmployeeId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role.Name == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(users, page.Page, page.PageSize, total);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken) => db.Users.AnyAsync(cancellationToken);
}
