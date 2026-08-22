using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Papasur.Application.Abstractions;
using Papasur.Application.Documentos.Commands.ConfirmarDocumento;
using Papasur.Application.Documentos.Commands.GenerarBorrador;
using Papasur.Application.Documentos.Inference;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;
using Papasur.Infrastructure.Audit;
using Papasur.Infrastructure.Documentos;
using Papasur.Infrastructure.Persistence;
using Papasur.Infrastructure.Trazabilidad;
using Testcontainers.PostgreSql;

namespace Papasur.Tests.Documentos;

/// <summary>
/// Test de INTEGRACIÓN del vertical completo contra Postgres 17 real (Testcontainers — requiere Docker):
/// migración → seed de trazabilidad → generar borrador → revisar → confirmar.
/// Cubre el mapeo EF (snake_case, FKs, enum a string) y los Include de los repos, que es donde el
/// test unitario con fakes no llega.
/// </summary>
public sealed class CopilotoFlujoIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
        await new TrazabilidadSeeder(db, NullLogger<TrazabilidadSeeder>.Instance).SeedAsync();
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
    public async Task Seeder_SiembraTrazabilidadYPlantillaYEsIdempotente()
    {
        await using var db = CreateDbContext();

        var lotes = await db.Lotes.CountAsync();
        var movimientos = await db.Movimientos.CountAsync();
        var plantillas = await db.PlantillasDocumento.Include(p => p.Campos).ToListAsync();

        Assert.True(lotes > 0, "el seeder debería dejar lotes de demo");
        Assert.True(movimientos > 0, "el seeder debería dejar movimientos de demo");
        var proforma = Assert.Single(plantillas);
        Assert.True(proforma.Activa);
        Assert.NotEmpty(proforma.Campos);
        // La plantilla mezcla campos inferibles y campos que quedan para la persona.
        Assert.Contains(proforma.Campos, c => c.ReglaMapeo is not null);
        Assert.Contains(proforma.Campos, c => c.ReglaMapeo is null);

        // Segunda corrida: no duplica nada.
        await new TrazabilidadSeeder(db, NullLogger<TrazabilidadSeeder>.Instance).SeedAsync();
        Assert.Equal(lotes, await db.Lotes.CountAsync());
        Assert.Equal(movimientos, await db.Movimientos.CountAsync());
    }

    [Fact]
    public async Task FlujoCompleto_GenerarRevisarYConfirmar()
    {
        Guid documentoId;
        Guid loteId;
        Guid plantillaId;
        Guid movimientoId;

        await using (var db = CreateDbContext())
        {
            var lote = await db.Lotes
                .Include(l => l.Movimientos)
                .FirstAsync(l => l.Movimientos.Any());

            loteId = lote.Id;
            movimientoId = lote.Movimientos.First().Id;
            plantillaId = (await db.PlantillasDocumento.FirstAsync()).Id;
        }

        // 1) Generar el borrador con la trazabilidad real.
        await using (var db = CreateDbContext())
        {
            var handler = new GenerarBorradorCommandHandler(
                new EfLoteRepository(db),
                new EfPlantillaRepository(db),
                new EfDocumentoRepository(db),
                new MotorInferenciaReglas(),
                new EfAuditRepository(db));

            var result = await handler.Handle(
                new GenerarBorradorCommand(loteId, plantillaId, movimientoId),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            documentoId = result.Value;
        }

        // 2) Revisar: se persistieron los valores, con inferidos y manuales.
        await using (var db = CreateDbContext())
        {
            var documento = await new EfDocumentoRepository(db).GetByIdAsync(documentoId, CancellationToken.None);

            Assert.NotNull(documento);
            Assert.Equal(StatusIds.EnProceso, documento.StatusId);
            Assert.Null(documento.ConfirmedAt);
            Assert.NotEmpty(documento.Valores);
            // El Include tiene que traer el campo de plantilla: sin eso la validación de obligatorios explota.
            Assert.All(documento.Valores, v => Assert.NotNull(v.CampoPlantilla));

            var inferidos = documento.Valores.Where(v => v.Origen == OrigenValor.Inferido).ToList();
            Assert.NotEmpty(inferidos);
            Assert.All(inferidos, v =>
            {
                Assert.False(string.IsNullOrWhiteSpace(v.Valor));
                Assert.False(string.IsNullOrWhiteSpace(v.InferidoDesde));
            });
        }

        // 3) Confirmar sin completar los obligatorios vacíos: tiene que rechazar.
        await using (var db = CreateDbContext())
        {
            var repo = new EfDocumentoRepository(db);
            var handler = new ConfirmarDocumentoCommandHandler(repo, new EfAuditRepository(db));

            var documento = await repo.GetByIdAsync(documentoId, CancellationToken.None);
            var faltantes = documento!.Valores
                .Where(v => v.CampoPlantilla.Obligatorio && string.IsNullOrWhiteSpace(v.Valor))
                .ToList();

            if (faltantes.Count > 0)
            {
                var rechazo = await handler.Handle(
                    new ConfirmarDocumentoCommand(documentoId, []),
                    CancellationToken.None);

                Assert.True(rechazo.IsFailure);
                Assert.Equal("Documento.CamposObligatorios", rechazo.Error.Code);
            }
        }

        // 4) Confirmar completando lo que falta.
        await using (var db = CreateDbContext())
        {
            var repo = new EfDocumentoRepository(db);
            var handler = new ConfirmarDocumentoCommandHandler(repo, new EfAuditRepository(db));

            var documento = await repo.GetByIdAsync(documentoId, CancellationToken.None);
            var ediciones = documento!.Valores
                .Where(v => v.CampoPlantilla.Obligatorio && string.IsNullOrWhiteSpace(v.Valor))
                .Select(v => new CampoEdicion(v.CampoPlantillaId, $"completado {v.CampoPlantilla.Clave}"))
                .ToList();

            var result = await handler.Handle(
                new ConfirmarDocumentoCommand(documentoId, ediciones),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        // 5) Quedó finalizado y confirmado en la base.
        await using (var db = CreateDbContext())
        {
            var documento = await new EfDocumentoRepository(db).GetByIdAsync(documentoId, CancellationToken.None);

            Assert.NotNull(documento);
            Assert.Equal(StatusIds.Finalizado, documento.StatusId);
            Assert.NotNull(documento.ConfirmedAt);
            Assert.All(documento.Valores, v => Assert.True(v.Confirmado));

            // Y no se puede volver a confirmar.
            var handler = new ConfirmarDocumentoCommandHandler(
                new EfDocumentoRepository(db), new EfAuditRepository(db));

            var repetido = await handler.Handle(
                new ConfirmarDocumentoCommand(documentoId, []),
                CancellationToken.None);

            Assert.True(repetido.IsFailure);
            Assert.Equal("Documento.YaConfirmado", repetido.Error.Code);
        }
    }

    [Fact]
    public async Task EfLoteRepository_PaginaYFiltra()
    {
        await using var db = CreateDbContext();
        var repo = new EfLoteRepository(db);

        var page = await repo.ListAsync(new PageRequest(1, 2), null, null, CancellationToken.None);

        Assert.Equal(2, page.PageSize);
        Assert.True(page.TotalCount >= page.Items.Count);
        Assert.All(page.Items, l => Assert.NotNull(l.Variedad));

        var primero = page.Items.First();
        var porCodigo = await repo.ListAsync(
            new PageRequest(1, 20), primero.Codigo, null, CancellationToken.None);

        Assert.Contains(porCodigo.Items, l => l.Id == primero.Id);

        var detalle = await repo.GetByIdAsync(primero.Id, CancellationToken.None);
        Assert.NotNull(detalle);
        Assert.NotNull(detalle.Variedad);
    }
}
