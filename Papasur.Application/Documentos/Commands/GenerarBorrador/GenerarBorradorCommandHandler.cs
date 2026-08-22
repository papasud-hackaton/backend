using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Documentos.Inference;
using Papasur.Application.Documentos.Ports;
using Papasur.Application.Trazabilidad.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Documentos.Commands.GenerarBorrador;

public sealed class GenerarBorradorCommandHandler(
    ILoteRepository lotes,
    IPlantillaRepository plantillas,
    IDocumentoRepository documentos,
    IMotorInferencia motor,
    IAuditRepository audit)
    : ICommandHandler<GenerarBorradorCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerarBorradorCommand command, CancellationToken cancellationToken)
    {
        var lote = await lotes.GetByIdAsync(command.LoteId, cancellationToken);

        if (lote is null)
        {
            return Result.Failure<Guid>(new Error("Lote.NotFound", "El lote indicado no existe."));
        }

        var plantilla = await plantillas.GetByIdAsync(command.PlantillaDocumentoId, cancellationToken);

        if (plantilla is null)
        {
            return Result.Failure<Guid>(new Error("Plantilla.NotFound", "La plantilla indicada no existe."));
        }

        if (!plantilla.Activa)
        {
            return Result.Failure<Guid>(new Error("Plantilla.Inactiva", "La plantilla no está activa."));
        }

        // El movimiento es opcional; si se indica, tiene que pertenecer al lote.
        Movimiento? movimiento = null;

        if (command.MovimientoId is { } movId)
        {
            movimiento = lote.Movimientos.FirstOrDefault(m => m.Id == movId);

            if (movimiento is null)
            {
                return Result.Failure<Guid>(
                    new Error("Movimiento.NotFound", "El movimiento indicado no pertenece al lote."));
            }
        }

        var documentoId = Guid.NewGuid();

        var valores = plantilla.Campos
            .OrderBy(c => c.Orden)
            .Select(campo =>
            {
                var inferido = motor.Inferir(campo.ReglaMapeo, lote, movimiento);

                return new ValorCampo
                {
                    Id = Guid.NewGuid(),
                    DocumentoExportacionId = documentoId,
                    CampoPlantillaId = campo.Id,
                    Valor = inferido?.Valor,
                    // Inferido cuando el sistema resolvió el valor; si no, queda pendiente de carga humana.
                    Origen = inferido is null ? OrigenValor.Manual : OrigenValor.Inferido,
                    Confirmado = false,
                    InferidoDesde = inferido?.InferidoDesde,
                };
            })
            .ToList();

        var documento = new DocumentoExportacion
        {
            Id = documentoId,
            LoteId = lote.Id,
            MovimientoId = movimiento?.Id,
            PlantillaDocumentoId = plantilla.Id,
            VersionPlantilla = plantilla.Version,
            StatusId = StatusIds.EnProceso,
            CreatedByUserId = command.PerformedByUserId,
            CreatedAt = DateTime.UtcNow,
            Valores = valores,
        };

        await documentos.AddAsync(documento, cancellationToken);

        // La auditoría referencia al usuario (FK restrict); sólo se registra si hay uno autenticado.
        if (command.PerformedByUserId is { } userId)
        {
            var inferidos = valores.Count(v => v.Origen == OrigenValor.Inferido);

            await audit.AddAsync(
                new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = AuditActions.DocumentGenerated,
                    EntityType = nameof(DocumentoExportacion),
                    EntityId = documentoId.ToString(),
                    Detail = $"Borrador {plantilla.Tipo} para lote {lote.Codigo}: {inferidos}/{valores.Count} campos inferidos.",
                    IpAddress = command.IpAddress,
                    OccurredAt = DateTime.UtcNow,
                },
                cancellationToken);
        }

        return Result.Success(documentoId);
    }
}
