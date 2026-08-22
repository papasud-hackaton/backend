using Papasur.Application.Abstractions;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Roles.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Tests.Fakes;

/// <summary>Fakes en memoria para probar handlers sin DB (tests unitarios).</summary>
public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(Users.Any(u => u.Email == email));

    public Task<bool> EmployeeNumberExistsAsync(string employeeNumber, CancellationToken cancellationToken)
        => Task.FromResult(Users.Any(u => u.EmployeeNumber == employeeNumber));

    public Task<PagedResult<User>> ListAsync(
        PageRequest page,
        string? search,
        int? roleId,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = Users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (roleId is { } role)
        {
            query = query.Where(u => u.RoleId == role);
        }

        if (isActive is { } active)
        {
            query = query.Where(u => u.IsActive == active);
        }

        var all = query.ToList();

        return Task.FromResult(new PagedResult<User>(
            all.Skip(page.Skip).Take(page.PageSize).ToList(),
            page.Page,
            page.PageSize,
            all.Count));
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Count > 0);
}

public sealed class FakeRoleRepository(params int[] existingRoleIds) : IRoleRepository
{
    private readonly int[] _ids = existingRoleIds.Length > 0
        ? existingRoleIds
        : [RoleIds.Admin, RoleIds.Supervisor, RoleIds.Agente];

    public Task<PagedResult<Role>> ListAsync(PageRequest page, CancellationToken cancellationToken)
        => Task.FromResult(new PagedResult<Role>([], page.Page, page.PageSize, 0));

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => Task.FromResult<Role?>(null);

    public Task<bool> ExistsAsync(int roleId, CancellationToken cancellationToken)
        => Task.FromResult(_ids.Contains(roleId));
}

public sealed class FakeAuditRepository : IAuditRepository
{
    public List<AuditEntry> Entries { get; } = [];

    public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditEntry>> ListAsync(
        PageRequest page,
        AuditFilter filter,
        CancellationToken cancellationToken)
        => Task.FromResult(new PagedResult<AuditEntry>(Entries, page.Page, page.PageSize, Entries.Count));
}

/// <summary>Hasher trivial: NO usar fuera de tests.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
}

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public AccessToken Generate(User user)
        => new($"token-for-{user.Email}", DateTime.UtcNow.AddHours(1));
}
