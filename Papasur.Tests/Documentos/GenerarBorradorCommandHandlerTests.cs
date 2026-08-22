using Papasur.Application.Documentos.Commands.GenerarBorrador;
using Papasur.Application.Documentos.Inference;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;
using Papasur.Domain.Trazabilidad;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Documentos;

public class GenerarBorradorCommandHandlerTests
{
    private readonly FakeLoteRepository _lotes = new();
    private readonly FakePlantillaRepository _plantillas = new();
    private readonly FakeDocumentoRepository _documentos = new();
    private readonly FakeAuditRepository _audit = new();

    private readonly Lote _lote;
    private readonly Movimiento _movimiento;
    private readonly PlantillaDocumento _plantilla;

    public GenerarBorradorCommandHandlerTests()
    {
        _lote = new Lote
        {
            Id = Guid.NewGuid(),
            Codigo = "224",
            Variedad = new Variedad { Id = Guid.NewGuid(), Nombre = "agata" },
            Categoria = "exportacion",
        };

        _movimiento = new Movimiento
        {
            Id = Guid.NewGuid(),
            LoteId = _lote.Id,
            Tipo = TiposMovimiento.EntregaCliente,
            NumeroRemito = "805",
            Fecha = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc),
            Kilogramos = 29120m,
            Dtv = "13250335-4",
        };

        _lote.Movimientos.Add(_movimiento);
        _lotes.Lotes.Add(_lote);

        _plantilla = new PlantillaDocumento
        {
            Id = Guid.NewGuid(),
            Nombre = "Proforma de exportación de semilla",
            Tipo = TiposDocumento.Proforma,
            Version = 3,
            Activa = true,
            Campos =
            [
                Campo("lote", "Lote", orden: 0, regla: "lote.codigo", obligatorio: true),
                Campo("dtv", "DTV", orden: 1, regla: "movimiento.dtv", obligatorio: true),
                Campo("exportador", "Exportador", orden: 2, regla: null, obligatorio: true),
            ],
        };

        _plantillas.Plantillas.Add(_plantilla);
    }

    private static CampoPlantilla Campo(string clave, string etiqueta, int orden, string? regla, bool obligatorio)
        => new()
        {
            Id = Guid.NewGuid(),
            Clave = clave,
            Etiqueta = etiqueta,
            Orden = orden,
            ReglaMapeo = regla,
            Obligatorio = obligatorio,
            TipoDato = TiposDato.Texto,
        };

    private GenerarBorradorCommandHandler Handler() =>
        new(_lotes, _plantillas, _documentos, new MotorInferenciaReglas(), _audit);

    [Fact]
    public async Task Handle_PreCompletaLoInferibleYDejaElRestoParaElHumano()
    {
        var admin = Guid.NewGuid();

        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, _movimiento.Id) { PerformedByUserId = admin },
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var documento = Assert.Single(_documentos.Documentos);
        Assert.Equal(result.Value, documento.Id);
        Assert.Equal(StatusIds.EnProceso, documento.StatusId);
        Assert.Equal(_plantilla.Version, documento.VersionPlantilla);
        Assert.Equal(_movimiento.Id, documento.MovimientoId);
        Assert.Null(documento.ConfirmedAt);

        // Un valor por cada campo de la plantilla, en el orden declarado.
        Assert.Equal(3, documento.Valores.Count);

        var lote = documento.Valores.ElementAt(0);
        Assert.Equal("224", lote.Valor);
        Assert.Equal(OrigenValor.Inferido, lote.Origen);
        Assert.Equal("lote.codigo", lote.InferidoDesde);

        var dtv = documento.Valores.ElementAt(1);
        Assert.Equal("13250335-4", dtv.Valor);
        Assert.Equal(OrigenValor.Inferido, dtv.Origen);

        // Sin regla de mapeo: queda vacío y a cargo de la persona.
        var exportador = documento.Valores.ElementAt(2);
        Assert.Null(exportador.Valor);
        Assert.Equal(OrigenValor.Manual, exportador.Origen);
        Assert.Null(exportador.InferidoDesde);

        // Nada se da por confirmado al generar.
        Assert.All(documento.Valores, v => Assert.False(v.Confirmado));
    }

    [Fact]
    public async Task Handle_SinMovimiento_SoloInfiereLoDelLote()
    {
        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var documento = Assert.Single(_documentos.Documentos);
        Assert.Null(documento.MovimientoId);
        Assert.Equal("224", documento.Valores.ElementAt(0).Valor);
        Assert.Null(documento.Valores.ElementAt(1).Valor);
        Assert.Equal(OrigenValor.Manual, documento.Valores.ElementAt(1).Origen);
    }

    [Fact]
    public async Task Handle_RegistraAuditoriaConElConteoDeInferidos()
    {
        var admin = Guid.NewGuid();

        await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, _movimiento.Id)
            {
                PerformedByUserId = admin,
                IpAddress = "10.0.0.7",
            },
            CancellationToken.None);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.DocumentGenerated, entry.Action);
        Assert.Equal(admin, entry.UserId);
        Assert.Equal("10.0.0.7", entry.IpAddress);
        Assert.Contains("2/3", entry.Detail);
    }

    [Fact]
    public async Task Handle_SinUsuarioAutenticado_NoAuditaPeroGenera()
    {
        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, _movimiento.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_documentos.Documentos);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Handle_LoteInexistente_DevuelveFailure()
    {
        var result = await Handler().Handle(
            new GenerarBorradorCommand(Guid.NewGuid(), _plantilla.Id, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lote.NotFound", result.Error.Code);
        Assert.Empty(_documentos.Documentos);
    }

    [Fact]
    public async Task Handle_PlantillaInexistente_DevuelveFailure()
    {
        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plantilla.NotFound", result.Error.Code);
        Assert.Empty(_documentos.Documentos);
    }

    [Fact]
    public async Task Handle_PlantillaInactiva_NoGenera()
    {
        _plantilla.Activa = false;

        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plantilla.Inactiva", result.Error.Code);
        Assert.Empty(_documentos.Documentos);
    }

    [Fact]
    public async Task Handle_MovimientoDeOtroLote_NoGenera()
    {
        var result = await Handler().Handle(
            new GenerarBorradorCommand(_lote.Id, _plantilla.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Movimiento.NotFound", result.Error.Code);
        Assert.Empty(_documentos.Documentos);
    }
}
