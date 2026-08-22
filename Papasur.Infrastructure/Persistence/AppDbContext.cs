using Papasur.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// DbContext ÚNICO del sistema. Todas las migraciones viven en Persistence/Migrations
/// (una sola carpeta, un solo __EFMigrationsHistory en schema public) — registro limpio.
/// Naming convention: snake_case (configurado en DependencyInjection).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("item");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Valor).HasPrecision(18, 4);
            entity.HasIndex(i => i.Nombre);
        });
    }
}
