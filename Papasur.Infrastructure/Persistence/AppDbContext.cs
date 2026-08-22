using Microsoft.EntityFrameworkCore;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.ExportForms;
using Papasur.Domain.Inventory;
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

    // Formularios de exportación (el envío completo: contrato §5).
    public DbSet<ExportForm> ExportForms => Set<ExportForm>();

    public DbSet<ExportFormItem> ExportFormItems => Set<ExportFormItem>();

    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();

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
            entity.Property(c => c.TaxId).HasMaxLength(50);
            entity.Property(c => c.CountryCode).HasMaxLength(2);
            entity.Property(c => c.Address).HasMaxLength(300);
            entity.Property(c => c.City).HasMaxLength(150);
            entity.Property(c => c.ContactName).HasMaxLength(150);
            entity.Property(c => c.ContactEmail).HasMaxLength(200);
            entity.Property(c => c.DefaultIncoterm).HasMaxLength(10);
            entity.Property(c => c.DefaultPortOfDischarge).HasMaxLength(150);
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

            entity.Property(l => l.Posicion).HasMaxLength(50);
            entity.Property(l => l.PoderGerminativo).HasPrecision(5, 2);
            entity.Property(l => l.Pureza).HasPrecision(5, 2);
            entity.Property(l => l.Humedad).HasPrecision(5, 2);
            entity.Property(l => l.Tratamiento).HasMaxLength(200);
            entity.Property(l => l.RegistroInase).HasMaxLength(100);

            entity.HasOne(l => l.Campo)
                .WithMany(c => c.Lotes)
                .HasForeignKey(l => l.CampoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.StorageLocation)
                .WithMany(s => s.Lotes)
                .HasForeignKey(l => l.StorageLocationId)
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
            entity.Property(p => p.Codigo).HasMaxLength(60).IsRequired();
            entity.Property(p => p.Ambito).HasMaxLength(20).IsRequired();
            entity.HasIndex(p => new { p.Nombre, p.Version }).IsUnique();
            entity.HasIndex(p => p.Tipo);
            entity.HasIndex(p => new { p.Ambito, p.Codigo });
        });

        modelBuilder.Entity<CampoPlantilla>(entity =>
        {
            entity.ToTable("campo_plantilla");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Clave).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Etiqueta).HasMaxLength(200).IsRequired();
            entity.Property(c => c.TipoDato).HasMaxLength(30).IsRequired();
            entity.Property(c => c.ReglaMapeo).HasMaxLength(300);
            entity.Property(c => c.Origen).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Ayuda).HasMaxLength(300);
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

            entity.HasIndex(d => d.ExportFormId);

            entity.HasOne(d => d.Lote)
                .WithMany()
                .HasForeignKey(d => d.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Los documentos de un envío caen con el envío: sin formulario no significan nada.
            entity.HasOne(d => d.ExportForm)
                .WithMany(f => f.Documents)
                .HasForeignKey(d => d.ExportFormId)
                .OnDelete(DeleteBehavior.Cascade);

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

        // ---------- Ubicaciones de stock ----------

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.ToTable("storage_location");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Code).HasMaxLength(20).IsRequired();
            entity.Property(l => l.Name).HasMaxLength(150).IsRequired();
            entity.Property(l => l.Type).HasMaxLength(20).IsRequired();
            entity.Property(l => l.TemperatureC).HasPrecision(5, 2);
            entity.HasIndex(l => l.Code).IsUnique();
        });

        // ---------- Formularios de exportación ----------

        modelBuilder.Entity<ExportForm>(entity =>
        {
            entity.ToTable("export_form");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Code).HasMaxLength(30).IsRequired();
            entity.Property(f => f.Status).HasMaxLength(30).IsRequired();
            entity.Property(f => f.DestinationCountryCode).HasMaxLength(2);
            entity.Property(f => f.PortOfLoading).HasMaxLength(150);
            entity.Property(f => f.PortOfDischarge).HasMaxLength(150);
            entity.Property(f => f.Incoterm).HasMaxLength(10).IsRequired();
            entity.Property(f => f.Currency).HasMaxLength(3).IsRequired();
            entity.Property(f => f.PaymentTerms).HasMaxLength(300);
            entity.Property(f => f.Notes).HasMaxLength(2000);
            entity.Property(f => f.ReviewNotes).HasMaxLength(2000);
            // Los valores de requisitos son un diccionario abierto: jsonb, no una tabla por campo.
            entity.Property(f => f.RequirementValues).HasColumnType("jsonb");

            entity.HasIndex(f => f.Code).IsUnique();
            entity.HasIndex(f => f.Status);
            entity.HasIndex(f => f.CreatedByUserId);
            entity.HasIndex(f => f.CreatedAt);

            entity.HasOne(f => f.Customer)
                .WithMany()
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict: un usuario con formularios no se borra (por eso se desactiva, no se elimina).
            entity.HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.ReviewedByUser)
                .WithMany()
                .HasForeignKey(f => f.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExportFormItem>(entity =>
        {
            entity.ToTable("export_form_item");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.QuantityKg).HasPrecision(18, 3);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 4);
            entity.Property(i => i.LineTotal).HasPrecision(18, 2);
            entity.Property(i => i.PackagingType).HasMaxLength(20).IsRequired();
            entity.HasIndex(i => i.ExportFormId);
            entity.HasIndex(i => i.LotId);

            // La línea no existe sin su formulario.
            entity.HasOne(i => i.ExportForm)
                .WithMany(f => f.Items)
                .HasForeignKey(i => i.ExportFormId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Lot)
                .WithMany()
                .HasForeignKey(i => i.LotId)
                .OnDelete(DeleteBehavior.Restrict);

            // La trazabilidad congelada son columnas de la misma fila: no tiene vida propia.
            entity.OwnsOne(i => i.Traceability, snapshot =>
            {
                snapshot.Property(t => t.LotCode).HasColumnName("traceability_lot_code").HasMaxLength(50);
                snapshot.Property(t => t.Species).HasColumnName("traceability_species").HasMaxLength(100);
                snapshot.Property(t => t.Variety).HasColumnName("traceability_variety").HasMaxLength(100);
                snapshot.Property(t => t.Category).HasColumnName("traceability_category").HasMaxLength(100);
                snapshot.Property(t => t.CropYear).HasColumnName("traceability_crop_year");
                snapshot.Property(t => t.LocationCode).HasColumnName("traceability_location_code").HasMaxLength(20);
                snapshot.Property(t => t.GerminationRate).HasColumnName("traceability_germination_rate").HasPrecision(5, 2);
                snapshot.Property(t => t.Purity).HasColumnName("traceability_purity").HasPrecision(5, 2);
                snapshot.Property(t => t.InaseRegistration).HasColumnName("traceability_inase_registration").HasMaxLength(100);
                snapshot.Property(t => t.CapturedAt).HasColumnName("traceability_captured_at");
            });
        });
    }
}
