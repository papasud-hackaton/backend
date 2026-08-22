using Microsoft.EntityFrameworkCore;
using Papasur.Domain.Audit;
using Papasur.Domain.Items;
using Papasur.Domain.Statuses;
using Papasur.Domain.Users;

namespace Papasur.Infrastructure.Persistence;

/// <summary>
/// DbContext ÚNICO del sistema. Todas las migraciones viven en Persistence/Migrations
/// (una sola carpeta, un solo __EFMigrationsHistory en schema public) — registro limpio.
/// Naming convention: snake_case (configurado en DependencyInjection).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Status> Statuses => Set<Status>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("role");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.Name).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(200).IsRequired();
            entity.HasIndex(r => r.Name).IsUnique();

            // Catálogo fijo: se siembra por migración, no se administra por API.
            entity.HasData(
                new Role { Id = RoleIds.Admin, Name = RoleNames.Admin, Description = "Acceso total al sistema." },
                new Role { Id = RoleIds.Supervisor, Name = RoleNames.Supervisor, Description = "Supervisa y consulta la operación." },
                new Role { Id = RoleIds.Agente, Name = RoleNames.Agente, Description = "Opera la documentación día a día." });
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("status");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.Code).HasMaxLength(50).IsRequired();
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(s => s.Code).IsUnique();

            // Catálogo fijo: las entidades con ciclo de vida referencian esta tabla por FK StatusId.
            entity.HasData(
                new Status { Id = StatusIds.EnProceso, Code = Domain.Statuses.StatusCodes.EnProceso, Name = "En proceso" },
                new Status { Id = StatusIds.Finalizado, Code = Domain.Statuses.StatusCodes.Finalizado, Name = "Finalizado" },
                new Status { Id = StatusIds.Cancelado, Code = Domain.Statuses.StatusCodes.Cancelado, Name = "Cancelado" });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(u => u.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.EmployeeNumber).IsUnique();

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entry");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.Detail).HasMaxLength(1000);
            entity.Property(a => a.IpAddress).HasMaxLength(64);

            // La auditoría es inmutable y nunca se borra en cascada con el agente.
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices pensados para los filtros del endpoint de consulta.
            entity.HasIndex(a => a.OccurredAt);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });
    }
}
