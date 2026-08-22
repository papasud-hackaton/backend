using Papasur.Application.Abstractions;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Domain.Users;
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

    public Task<bool> EmployeeIdExistsAsync(string employeeId, CancellationToken cancellationToken)
        => Task.FromResult(Users.Any(u => u.EmployeeId == employeeId));

    public Task<PagedResult<User>> ListAsync(
        PageRequest page,
        string? search,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = Users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                || u.EmployeeId.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role?.Name == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
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

/// <summary>Guarda los tokens de recuperación/invitación emitidos, para poder canjearlos en los tests.</summary>
public sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public List<PasswordResetToken> Tokens { get; } = [];

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task InvalidateAllForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId && t.UsedAt is null))
        {
            token.UsedAt = now;
        }

        return Task.CompletedTask;
    }
}

/// <summary>Captura los envíos en vez de mandar correo, para poder aseverar sobre ellos.</summary>
public sealed class FakeInvitationSender : IInvitationSender
{
    public List<(string Email, string Token, bool EsInvitacion)> Enviados { get; } = [];

    public Task SendInvitationAsync(string email, string firstName, string token, CancellationToken cancellationToken)
    {
        Enviados.Add((email, token, true));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string firstName, string token, CancellationToken cancellationToken)
    {
        Enviados.Add((email, token, false));
        return Task.CompletedTask;
    }
}

public sealed class FakeRoleRepository(params int[] existingRoleIds) : IRoleRepository
{
    private readonly int[] _ids = existingRoleIds.Length > 0
        ? existingRoleIds
        : [RoleIds.Admin, RoleIds.Supervisor, RoleIds.Agent];

    private static readonly Dictionary<string, int> PorNombre = new()
    {
        [RoleNames.Admin] = RoleIds.Admin,
        [RoleNames.Supervisor] = RoleIds.Supervisor,
        [RoleNames.Agent] = RoleIds.Agent,
    };

    public Task<PagedResult<Role>> ListAsync(PageRequest page, CancellationToken cancellationToken)
        => Task.FromResult(new PagedResult<Role>([], page.Page, page.PageSize, 0));

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => Task.FromResult(PorNombre.TryGetValue(name, out var id) && _ids.Contains(id)
            ? new Role { Id = id, Name = name }
            : null);

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

    public Task<IReadOnlyList<AuditEntry>> ListAllAsync(AuditFilter filter, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AuditEntry>>(Entries);
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
