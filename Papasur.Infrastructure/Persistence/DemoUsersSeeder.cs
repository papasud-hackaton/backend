using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papasur.Application.Auth.Ports;
using Papasur.Domain.Users;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// Siembra las TRES cuentas de demostración que el front ofrece en los accesos rápidos del login
/// (una por rol). Sin esto la pantalla de entrada del front no tiene contra qué autenticarse.
///
/// SÓLO en Development, y sólo si la cuenta no existe. No es una puerta trasera: fuera de
/// Development no corre, y el alta real sigue siendo por invitación de un admin (ai.md §10).
/// </summary>
public sealed class DemoUsersSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DemoUsersSeeder> logger)
{
    /// <summary>La misma que documenta el front para las tres cuentas.</summary>
    public const string DemoPassword = "papasud";

    private static readonly (string Email, string FirstName, string LastName, string EmployeeId, int RoleId)[] Accounts =
    [
        ("martina.godoy@papasud.com.ar", "Martina", "Godoy", "1042", RoleIds.Agent),
        ("rodrigo.paz@papasud.com.ar", "Rodrigo", "Paz", "1017", RoleIds.Supervisor),
        ("elena.arrieta@papasud.com.ar", "Elena", "Arrieta", "1003", RoleIds.Admin),
    ];

    public async Task SeedAsync(bool isDevelopment, CancellationToken cancellationToken = default)
    {
        if (!isDevelopment)
        {
            return;
        }

        var existing = await db.Users
            .Where(u => Accounts.Select(a => a.Email).Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        var created = 0;
        var now = DateTime.UtcNow;

        foreach (var account in Accounts)
        {
            if (existing.Contains(account.Email))
            {
                continue;
            }

            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                PasswordHash = passwordHasher.Hash(DemoPassword),
                EmployeeId = account.EmployeeId,
                RoleId = account.RoleId,
                Status = UserStatuses.Active,
                CreatedAt = now,
            });

            created++;
        }

        if (created == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Sembradas {Count} cuentas de DEMOSTRACIÓN — no usar fuera de Development.", created);
    }
}
