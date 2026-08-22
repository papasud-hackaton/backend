using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Roles.Ports;
using Papasur.Domain.Users;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Roles;

public class EfRoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task<PagedResult<Role>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Roles.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Role>(roles, page.Page, page.PageSize, total);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public Task<bool> ExistsAsync(int roleId, CancellationToken cancellationToken)
        => db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
}
