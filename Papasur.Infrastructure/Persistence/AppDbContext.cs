using Microsoft.EntityFrameworkCore;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Items;
using Papasur.Domain.Statuses;
using Papasur.Domain.Trazabilidad;
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

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Status> Statuses => Set<Status>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    // Trazabilidad (fuente de verdad: la doc de exportación es una proyección sobre esto).
    public DbSet<Variedad> Variedades => Set<Variedad>();

    public DbSet<Campo> Campos => Set<Campo>();

    public DbSet<Transportista> Transportistas => Set<Transportista>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Lote> Lotes => Set<Lote>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    // Documentación de exportación (copiloto).
    public DbSet<PlantillaDocumento> PlantillasDocumento => Set<PlantillaDocumento>();

    public DbSet<CampoPlantilla> CamposPlantilla => Set<CampoPlantilla>();

    public DbSet<DocumentoExportacion> DocumentosExportacion => Set<DocumentoExportacion>();

    public DbSet<ValorCampo> ValoresCampo => Set<ValorCampo>();

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
                new Role { Id = RoleIds.Agent, Name = RoleNames.Agent, Description = "Opera la documentación día a día." });
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
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            // Vacío mientras el usuario está invitado y no definió contraseña.
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(u => u.EmployeeId).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(50);
            entity.Property(u => u.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.EmployeeId).IsUnique();
            entity.HasIndex(u => u.Status);
            // FullName e IsActive se calculan en el dominio: no son columnas.
            entity.Ignore(u => u.FullName);
            entity.Ignore(u => u.IsActive);

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_token");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            // Se busca por hash en cada canje: único e indexado.
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entry");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ActorName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.ActorRole).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            // JSON con los cambios: [{ field, from, to }].
            entity.Property(a => a.Changes).HasColumnType("jsonb");
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
            entity.HasIndex(a => a.ActorRole);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        // ---------- Trazabilidad ----------

        modelBuilder.Entity<Variedad>(entity =>
        {
            entity.ToTable("variedad");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Nombre).HasMaxLength(120).IsRequired();
            entity.HasIndex(v => v.Nombre).IsUnique();
        });

        modelBuilder.Entity<Campo>(entity =>
        {
            entity.ToTable("campo");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Establecimiento).HasMaxLength(150);
            entity.Property(c => c.Pivote).HasMaxLength(50);
            entity.Property(c => c.Cuadrante).HasMaxLength(50);
            entity.HasIndex(c => c.Nombre);
        });

        modelBuilder.Entity<Transportista>(entity =>
        {
            entity.ToTable("transportista");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Nombre).HasMaxLength(150).IsRequired();
            entity.HasIndex(t => t.Nombre).IsUnique();
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("cliente");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Pais).HasMaxLength(100);
            entity.HasIndex(c => c.Nombre);
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.ToTable("lote");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(l => l.Categoria).HasMaxLength(100);
            entity.Property(l => l.SuperficieHa).HasPrecision(10, 3);
            entity.HasIndex(l => l.Codigo);

            entity.HasOne(l => l.Variedad)
                .WithMany(v => v.Lotes)
                .HasForeignKey(l => l.VariedadId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Campo)
                .WithMany(c => c.Lotes)
                .HasForeignKey(l => l.CampoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.ToTable("movimiento");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Tipo).HasMaxLength(30).IsRequired();
            entity.Property(m => m.NumeroRemito).HasMaxLength(50).IsRequired();
            entity.Property(m => m.Kilogramos).HasPrecision(18, 3);
            entity.Property(m => m.KgPromedio).HasPrecision(10, 3);
            entity.Property(m => m.Presentacion).HasMaxLength(50);
            entity.Property(m => m.Categoria).HasMaxLength(100);
            entity.Property(m => m.Calibre).HasMaxLength(50);
            entity.Property(m => m.Comisionista).HasMaxLength(150);
            entity.Property(m => m.Destino).HasMaxLength(150);
            entity.Property(m => m.Dtv).HasMaxLength(100);
            entity.Property(m => m.Observaciones).HasMaxLength(1000);
            entity.HasIndex(m => m.NumeroRemito);
            entity.HasIndex(m => m.Fecha);
            entity.HasIndex(m => m.Tipo);
            entity.HasIndex(m => m.Dtv);

            entity.HasOne(m => m.Lote)
                .WithMany(l => l.Movimientos)
                .HasForeignKey(m => m.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Transportista)
                .WithMany(t => t.Movimientos)
                .HasForeignKey(m => m.TransportistaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Cliente)
                .WithMany(c => c.Movimientos)
                .HasForeignKey(m => m.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Documentación de exportación ----------

        modelBuilder.Entity<PlantillaDocumento>(entity =>
        {
            entity.ToTable("plantilla_documento");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Organismo).HasMaxLength(150);
            entity.Property(p => p.PaisDestino).HasMaxLength(100);
            entity.HasIndex(p => new { p.Nombre, p.Version }).IsUnique();
            entity.HasIndex(p => p.Tipo);
        });

        modelBuilder.Entity<CampoPlantilla>(entity =>
        {
            entity.ToTable("campo_plantilla");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Clave).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Etiqueta).HasMaxLength(200).IsRequired();
            entity.Property(c => c.TipoDato).HasMaxLength(30).IsRequired();
            entity.Property(c => c.ReglaMapeo).HasMaxLength(300);
            entity.HasIndex(c => new { c.PlantillaDocumentoId, c.Clave }).IsUnique();

            entity.HasOne(c => c.PlantillaDocumento)
                .WithMany(p => p.Campos)
                .HasForeignKey(c => c.PlantillaDocumentoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentoExportacion>(entity =>
        {
            entity.ToTable("documento_exportacion");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.LoteId);
            entity.HasIndex(d => d.StatusId);

            entity.HasOne(d => d.Lote)
                .WithMany()
                .HasForeignKey(d => d.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Movimiento)
                .WithMany(m => m.Documentos)
                .HasForeignKey(d => d.MovimientoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.PlantillaDocumento)
                .WithMany()
                .HasForeignKey(d => d.PlantillaDocumentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Status)
                .WithMany()
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ValorCampo>(entity =>
        {
            entity.ToTable("valor_campo");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Valor).HasMaxLength(2000);
            entity.Property(v => v.InferidoDesde).HasMaxLength(300);
            // El origen se guarda como texto legible ("Inferido"/"Manual"/"Dictado"), no como número.
            entity.Property(v => v.Origen).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(v => new { v.DocumentoExportacionId, v.CampoPlantillaId }).IsUnique();

            entity.HasOne(v => v.DocumentoExportacion)
                .WithMany(d => d.Valores)
                .HasForeignKey(v => v.DocumentoExportacionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.CampoPlantilla)
                .WithMany()
                .HasForeignKey(v => v.CampoPlantillaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
