using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Papasur.Application.Auth.Ports;
using Papasur.Domain.Users;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// Siembra el usuario administrador inicial (sólo si la tabla de usuarios está vacía).
/// Sin esto no habría forma de autenticarse para dar de alta el primer usuario.
/// Credenciales por config: Seed__AdminEmail / Seed__AdminPassword.
/// En Development, si no hay config, usa un default explícito de desarrollo.
/// Fuera de Development, si no hay config, NO crea nada (y lo avisa por log).
/// </summary>
public sealed class DatabaseSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public const string DevelopmentAdminEmail = "admin@papasur.local";

    public const string DevelopmentAdminPassword = "Admin.12345";

    public async Task SeedAsync(bool isDevelopment, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!isDevelopment)
            {
                logger.LogWarning(
                    "No hay usuarios y falta Seed__AdminEmail/Seed__AdminPassword: no se creó el admin inicial.");
                return;
            }

            email = DevelopmentAdminEmail;
            password = DevelopmentAdminPassword;
            logger.LogWarning("Sembrando admin de DESARROLLO {Email} — no usar fuera de Development.", email);
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = configuration["Seed:AdminFirstName"] ?? "Admin",
            LastName = configuration["Seed:AdminLastName"] ?? "Papasud",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(password),
            EmployeeId = configuration["Seed:AdminEmployeeId"]
                ?? configuration["Seed:AdminEmployeeNumber"]
                ?? "0001",
            RoleId = RoleIds.Admin,
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Usuario administrador inicial creado: {Email}", email);
    }
}
