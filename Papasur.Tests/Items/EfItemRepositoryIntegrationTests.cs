using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Domain.Items;
using Papasur.Infrastructure.Items;
using Papasur.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Papasur.Tests.Items;

/// <summary>
/// Test de INTEGRACIÓN contra Postgres 17 real (Testcontainers — requiere Docker corriendo).
/// El CI los excluye con --filter "FullyQualifiedName!~Integration"; correrlos localmente con:
///   dotnet test --filter "FullyQualifiedName~Integration"
/// </summary>
public sealed class EfItemRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

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
    public async Task AddYList_RoundTripContraPostgresReal()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            Nombre = "Integración",
            Valor = 99.99m,
            FechaRegistro = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await using (var db = CreateDbContext())
        {
            await new EfItemRepository(db).AddAsync(item, CancellationToken.None);
        }

        await using (var db = CreateDbContext())
        {
            var page = await new EfItemRepository(db).ListAsync(new PageRequest(1, 20), CancellationToken.None);
            Assert.Equal(1, page.TotalCount);
            var guardado = Assert.Single(page.Items);
            Assert.Equal(item.Id, guardado.Id);
            Assert.Equal("Integración", guardado.Nombre);
        }
    }
}
