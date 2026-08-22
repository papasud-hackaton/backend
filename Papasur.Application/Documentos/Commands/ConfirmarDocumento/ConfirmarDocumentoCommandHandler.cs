using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Documentos.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;

namespace Papasur.Application.Documentos.Commands.ConfirmarDocumento;

public sealed class ConfirmarDocumentoCommandHandler(
    IDocumentoRepository documentos,
    IAuditRepository audit)
    : ICommandHandler<ConfirmarDocumentoCommand, Result>
{
    public async Task<Result> Handle(ConfirmarDocumentoCommand command, CancellationToken cancellationToken)
    {
        var documento = await documentos.GetByIdAsync(command.DocumentoId, cancellationToken);

        if (documento is null)
        {
            return Result.Failure(new Error("Documento.NotFound", "El documento indicado no existe."));
        }

        if (documento.ConfirmedAt is not null)
        {
            return Result.Failure(new Error("Documento.YaConfirmado", "El documento ya fue confirmado."));
        }

        // Aplica las ediciones del usuario: el valor pasa a ser Manual (o Dictado) y queda para confirmar.
        var ediciones = command.Campos ?? [];

        foreach (var edicion in ediciones)
        {
            var valor = documento.Valores.FirstOrDefault(v => v.CampoPlantillaId == edicion.CampoPlantillaId);

            if (valor is null)
            {
                return Result.Failure(new Error(
                    "Documento.CampoInvalido",
                    "Se intentó editar un campo que no pertenece al documento."));
            }

            valor.Valor = string.IsNullOrWhiteSpace(edicion.Valor) ? null : edicion.Valor.Trim();
            valor.Origen = edicion.PorDictado ? OrigenValor.Dictado : OrigenValor.Manual;
            valor.InferidoDesde = null;
        }

        // Valida obligatorios: no se confirma un documento con campos requeridos vacíos.
        var faltantes = documento.Valores
            .Where(v => v.CampoPlantilla.Obligatorio && string.IsNullOrWhiteSpace(v.Valor))
            .Select(v => v.CampoPlantilla.Etiqueta)
            .ToList();

        if (faltantes.Count > 0)
        {
            return Result.Failure(new Error(
                "Documento.CamposObligatorios",
                $"Faltan campos obligatorios: {string.Join(", ", faltantes)}."));
        }

        // Confirmación explícita: se marca cada campo como revisado y se finaliza el documento.
        foreach (var valor in documento.Valores)
        {
            valor.Confirmado = true;
        }

        documento.ConfirmedAt = DateTime.UtcNow;
        documento.StatusId = StatusIds.Finalizado;

        await documentos.UpdateAsync(documento, cancellationToken);

        if (command.PerformedByUserId is { } userId)
        {
            await audit.AddAsync(
                new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = AuditActions.DocumentConfirmed,
                    EntityType = nameof(DocumentoExportacion),
                    EntityId = documento.Id.ToString(),
                    Detail = $"Documento confirmado ({documento.Valores.Count} campos).",
                    IpAddress = command.IpAddress,
                    OccurredAt = DateTime.UtcNow,
                },
                cancellationToken);
        }

        return Result.Success();
    }
}
