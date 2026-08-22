using Papasur.Application.Documentos.Commands.ConfirmarDocumento;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;
using Papasur.Tests.Fakes;

namespace Papasur.Tests.Documentos;

/// <summary>
/// La confirmación es el paso donde una persona se hace responsable de lo que el sistema sugirió.
/// Estos tests cubren esa garantía: nada se finaliza con obligatorios vacíos y todo queda auditado.
/// </summary>
public class ConfirmarDocumentoCommandHandlerTests
{
    private readonly FakeDocumentoRepository _documentos = new();
    private readonly FakeAuditRepository _audit = new();

    private readonly DocumentoExportacion _documento;
    private readonly CampoPlantilla _campoInferido;
    private readonly CampoPlantilla _campoManualObligatorio;

    public ConfirmarDocumentoCommandHandlerTests()
    {
        _campoInferido = new CampoPlantilla
        {
            Id = Guid.NewGuid(),
            Clave = "dtv",
            Etiqueta = "DTV",
            Obligatorio = true,
            ReglaMapeo = "movimiento.dtv",
            Orden = 0,
        };

        _campoManualObligatorio = new CampoPlantilla
        {
            Id = Guid.NewGuid(),
            Clave = "exportador",
            Etiqueta = "Exportador",
            Obligatorio = true,
            Orden = 1,
        };

        _documento = new DocumentoExportacion
        {
            Id = Guid.NewGuid(),
            LoteId = Guid.NewGuid(),
            PlantillaDocumentoId = Guid.NewGuid(),
            VersionPlantilla = 1,
            StatusId = StatusIds.EnProceso,
            CreatedAt = DateTime.UtcNow,
            Valores =
            [
                new ValorCampo
                {
                    Id = Guid.NewGuid(),
                    CampoPlantillaId = _campoInferido.Id,
                    CampoPlantilla = _campoInferido,
                    Valor = "13250335-4",
                    Origen = OrigenValor.Inferido,
                    InferidoDesde = "movimiento.dtv",
                },
                new ValorCampo
                {
                    Id = Guid.NewGuid(),
                    CampoPlantillaId = _campoManualObligatorio.Id,
                    CampoPlantilla = _campoManualObligatorio,
                    Valor = null,
                    Origen = OrigenValor.Manual,
                },
            ],
        };

        _documentos.Documentos.Add(_documento);
    }

    private ConfirmarDocumentoCommandHandler Handler() => new(_documentos, _audit);

    private ConfirmarDocumentoCommand Command(params CampoEdicion[] campos)
        => new(_documento.Id, campos) { PerformedByUserId = Guid.NewGuid() };

    [Fact]
    public async Task Handle_ConObligatoriosCompletos_FinalizaYMarcaTodoConfirmado()
    {
        var result = await Handler().Handle(
            Command(new CampoEdicion(_campoManualObligatorio.Id, "Papasud SA")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusIds.Finalizado, _documento.StatusId);
        Assert.NotNull(_documento.ConfirmedAt);
        Assert.All(_documento.Valores, v => Assert.True(v.Confirmado));
        Assert.Equal(1, _documentos.Updates);

        var editado = _documento.Valores.Single(v => v.CampoPlantillaId == _campoManualObligatorio.Id);
        Assert.Equal("Papasud SA", editado.Valor);
        Assert.Equal(OrigenValor.Manual, editado.Origen);
    }

    [Fact]
    public async Task Handle_ConDictado_MarcaElOrigenComoDictado()
    {
        await Handler().Handle(
            Command(new CampoEdicion(_campoManualObligatorio.Id, "  Papasud SA  ", PorDictado: true)),
            CancellationToken.None);

        var editado = _documento.Valores.Single(v => v.CampoPlantillaId == _campoManualObligatorio.Id);
        Assert.Equal(OrigenValor.Dictado, editado.Origen);
        Assert.Equal("Papasud SA", editado.Valor);
    }

    [Fact]
    public async Task Handle_AlEditarUnInferido_PierdeLaTrazaDeInferencia()
    {
        await Handler().Handle(
            Command(
                new CampoEdicion(_campoInferido.Id, "13250335-9"),
                new CampoEdicion(_campoManualObligatorio.Id, "Papasud SA")),
            CancellationToken.None);

        var corregido = _documento.Valores.Single(v => v.CampoPlantillaId == _campoInferido.Id);
        Assert.Equal("13250335-9", corregido.Valor);
        Assert.Equal(OrigenValor.Manual, corregido.Origen);
        Assert.Null(corregido.InferidoDesde);
    }

    [Fact]
    public async Task Handle_ConObligatorioVacio_NoFinaliza()
    {
        var result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Documento.CamposObligatorios", result.Error.Code);
        Assert.Contains("Exportador", result.Error.Message);
        Assert.Equal(StatusIds.EnProceso, _documento.StatusId);
        Assert.Null(_documento.ConfirmedAt);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Handle_AlBorrarUnObligatorioInferido_NoFinaliza()
    {
        var result = await Handler().Handle(
            Command(
                new CampoEdicion(_campoInferido.Id, "   "),
                new CampoEdicion(_campoManualObligatorio.Id, "Papasud SA")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Documento.CamposObligatorios", result.Error.Code);
        Assert.Contains("DTV", result.Error.Message);
    }

    [Fact]
    public async Task Handle_ConCampoAjeno_DevuelveFailure()
    {
        var result = await Handler().Handle(
            Command(new CampoEdicion(Guid.NewGuid(), "cualquier cosa")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Documento.CampoInvalido", result.Error.Code);
        Assert.Null(_documento.ConfirmedAt);
    }

    [Fact]
    public async Task Handle_DocumentoYaConfirmado_NoSeVuelveAConfirmar()
    {
        _documento.ConfirmedAt = DateTime.UtcNow.AddMinutes(-5);

        var result = await Handler().Handle(
            Command(new CampoEdicion(_campoManualObligatorio.Id, "Otra empresa")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Documento.YaConfirmado", result.Error.Code);
        // Y no se aplicó la edición.
        Assert.Null(_documento.Valores.Single(v => v.CampoPlantillaId == _campoManualObligatorio.Id).Valor);
    }

    [Fact]
    public async Task Handle_DocumentoInexistente_DevuelveNotFound()
    {
        var result = await Handler().Handle(
            new ConfirmarDocumentoCommand(Guid.NewGuid(), []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Documento.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RegistraLaConfirmacionEnAuditoria()
    {
        var command = Command(new CampoEdicion(_campoManualObligatorio.Id, "Papasud SA"));

        await Handler().Handle(command, CancellationToken.None);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.DocumentConfirmed, entry.Action);
        Assert.Equal(command.PerformedByUserId, entry.UserId);
        Assert.Equal(_documento.Id.ToString(), entry.EntityId);
    }
}
