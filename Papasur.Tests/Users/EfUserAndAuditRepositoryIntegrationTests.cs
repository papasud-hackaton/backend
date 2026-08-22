using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Audit.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;
using Papasur.Infrastructure.Audit;
using Papasur.Infrastructure.Persistence;
using Papasur.Infrastructure.Users;
using Testcontainers.PostgreSql;

namespace Papasur.Tests.Users;

/// <summary>
/// Test de INTEGRACIÓN contra Postgres 17 real (Testcontainers — requiere Docker corriendo):
/// verifica el seed de roles/estados, la paginación con filtros de usuarios y los filtros
/// del endpoint de auditoría.
/// </summary>
public sealed class EfUserAndAuditRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Migracion_SiembraRolesYEstados()
    {
        await using var db = CreateDbContext();

        var roles = await db.Roles.OrderBy(r => r.Id).Select(r => r.Name).ToListAsync();
        var statuses = await db.Statuses.OrderBy(s => s.Id).Select(s => s.Code).ToListAsync();

        Assert.Equal([RoleNames.Admin, RoleNames.Supervisor, RoleNames.Agent], roles);
        Assert.Equal(
            [Domain.Statuses.StatusCodes.EnProceso, Domain.Statuses.StatusCodes.Finalizado, Domain.Statuses.StatusCodes.Cancelado],
            statuses);
    }

    [Fact]
    public async Task ListAsync_PaginaYFiltraUsuarios()
    {
        await SeedUsersAsync(count: 7, roleId: RoleIds.Agent);
        await SeedUsersAsync(count: 3, roleId: RoleIds.Supervisor, prefix: "sup");

        await using var db = CreateDbContext();
        var repo = new EfUserRepository(db);

        var firstPage = await repo.ListAsync(new PageRequest(1, 4), null, null, null, CancellationToken.None);
        Assert.Equal(10, firstPage.Total);
        Assert.Equal(4, firstPage.Items.Count);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.True(firstPage.HasNext);
        Assert.False(firstPage.HasPrevious);

        var lastPage = await repo.ListAsync(new PageRequest(3, 4), null, null, null, CancellationToken.None);
        Assert.Equal(2, lastPage.Items.Count);
        Assert.False(lastPage.HasNext);

        var byRole = await repo.ListAsync(
            new PageRequest(1, 20), null, RoleNames.Supervisor, null, CancellationToken.None);
        Assert.Equal(3, byRole.Total);
        Assert.All(byRole.Items, u => Assert.Equal(RoleNames.Supervisor, u.Role.Name));

        // Búsqueda case-insensitive sobre nombre, correo o legajo.
        var bySearch = await repo.ListAsync(
            new PageRequest(1, 20), "SUP-", null, null, CancellationToken.None);
        Assert.Equal(3, bySearch.Total);
    }

    [Fact]
    public async Task AuditListAsync_FiltraPorAgenteAccionYRangoDeFechas()
    {
        var userId = (await SeedUsersAsync(count: 1, roleId: RoleIds.Agent, prefix: "aud")).Single();
        var otherId = (await SeedUsersAsync(count: 1, roleId: RoleIds.Admin, prefix: "otro")).Single();

        var baseTime = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

        await using (var db = CreateDbContext())
        {
            var repo = new EfAuditRepository(db);

            await repo.AddAsync(NewEntry(userId, AuditActions.UserLogin, baseTime), CancellationToken.None);
            await repo.AddAsync(NewEntry(userId, AuditActions.UserLogout, baseTime.AddDays(1)), CancellationToken.None);
            await repo.AddAsync(NewEntry(userId, AuditActions.UserCreated, baseTime.AddDays(5)), CancellationToken.None);
            await repo.AddAsync(NewEntry(otherId, AuditActions.UserLogin, baseTime.AddDays(2)), CancellationToken.None);
        }

        await using var read = CreateDbContext();
        var audit = new EfAuditRepository(read);
        var page = new PageRequest(1, 20);

        var all = await audit.ListAsync(page, new AuditFilter(), CancellationToken.None);
        Assert.Equal(4, all.Total);
        // Orden por defecto: más reciente primero.
        Assert.Equal(baseTime.AddDays(5), all.Items[0].OccurredAt);
        Assert.Equal("Actor de prueba", all.Items[0].ActorName);

        var byUser = await audit.ListAsync(page, new AuditFilter(ActorId: userId), CancellationToken.None);
        Assert.Equal(3, byUser.Total);

        var byAction = await audit.ListAsync(
            page, new AuditFilter(Actions: [AuditActions.UserLogin]), CancellationToken.None);
        Assert.Equal(2, byAction.Total);

        var byRange = await audit.ListAsync(
            page,
            new AuditFilter(From: baseTime.AddDays(1), To: baseTime.AddDays(3)),
            CancellationToken.None);
        Assert.Equal(2, byRange.Total);

        var combined = await audit.ListAsync(
            page,
            new AuditFilter(ActorId: userId, Actions: [AuditActions.UserLogin], From: baseTime.AddDays(-1)),
            CancellationToken.None);
        Assert.Single(combined.Items);

        var paged = await audit.ListAsync(new PageRequest(2, 2), new AuditFilter(), CancellationToken.None);
        Assert.Equal(4, paged.Total);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(2, paged.TotalPages);
    }

    private static AuditEntry NewEntry(Guid userId, string action, DateTime occurredAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ActorName = "Actor de prueba",
        ActorRole = RoleNames.Agent,
        Action = action,
        EntityType = AuditEntityTypes.User,
        EntityId = userId.ToString(),
        OccurredAt = occurredAt,
    };

    private async Task<List<Guid>> SeedUsersAsync(int count, int roleId, string prefix = "user")
    {
        await using var db = CreateDbContext();
        var repo = new EfUserRepository(db);
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);

            await repo.AddAsync(
                new User
                {
                    Id = id,
                    FirstName = prefix,
                    LastName = $"Apellido {i}",
                    Email = $"{prefix}{i}@papasud.com",
                    PasswordHash = "irrelevante-para-este-test",
                    EmployeeId = $"{prefix.ToUpperInvariant()}-{i:D4}",
                    RoleId = roleId,
                    Status = UserStatuses.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                CancellationToken.None);
        }

        return ids;
    }
}
