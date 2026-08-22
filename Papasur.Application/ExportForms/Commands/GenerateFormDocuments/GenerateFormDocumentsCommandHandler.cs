using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Customers.Ports;
using Papasur.Application.Documentos.Ports;
using Papasur.Application.ExportForms.Inference;
using Papasur.Application.ExportForms.Ports;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Domain.Audit;
using Papasur.Domain.Documentos;
using Papasur.Domain.Statuses;

namespace Papasur.Application.ExportForms.Commands.GenerateFormDocuments;

/// <summary>
/// Acá se cruzan las dos mitades del producto: los requisitos documentales (que viven como DATO
/// en plantilla_documento) con la trazabilidad congelada del envío. De cada campo queda registrado
/// si lo puso el sistema, la persona o el dictado, y de qué regla salió — que es lo que permite
/// responder después "¿este dato quién lo puso?".
/// </summary>
public sealed class GenerateFormDocumentsCommandHandler(
    IExportFormRepository forms,
    IPlantillaRepository plantillas,
    IDocumentoRepository documentos,
    ICustomerRepository customers,
    IFormInferenceEngine engine,
    IAuditRepository audit)
    : ICommandHandler<GenerateFormDocumentsCommand, Result<IReadOnlyList<GeneratedDocumentDto>>>
{
    public async Task<Result<IReadOnlyList<GeneratedDocumentDto>>> Handle(
        GenerateFormDocumentsCommand command,
        CancellationToken cancellationToken)
    {
        var form = await forms.GetByIdAsync(command.FormId, cancellationToken);

        if (form is null)
        {
            return Result.Failure<IReadOnlyList<GeneratedDocumentDto>>(
                new Error("Form.NotFound", "Formulario no encontrado."));
        }

        if (form.Items.Count == 0)
        {
            return Result.Failure<IReadOnlyList<GeneratedDocumentDto>>(
                new Error("Form.NoItems", "Agregá al menos una línea antes de generar la documentación."));
        }

        var templates = await plantillas.ListByAmbitoAsync(AmbitosPlantilla.Formulario, cancellationToken);

        if (templates.Count == 0)
        {
            return Result.Failure<IReadOnlyList<GeneratedDocumentDto>>(
                new Error("Form.NoTemplates", "No hay requisitos documentales cargados."));
        }

        var customer = form.CustomerId is { } customerId
            ? await customers.GetByIdAsync(customerId, cancellationToken)
            : null;

        var manual = FormAssembler.ReadRequirementValues(form.RequirementValues);
        var now = DateTime.UtcNow;
        var generated = new List<DocumentoExportacion>(templates.Count);

        foreach (var template in templates)
        {
            var document = new DocumentoExportacion
            {
                Id = Guid.NewGuid(),
                ExportFormId = form.Id,
                PlantillaDocumentoId = template.Id,
                VersionPlantilla = template.Version,
                StatusId = StatusIds.EnProceso,
                CreatedByUserId = command.Actor.Id,
                CreatedAt = now,
            };

            foreach (var field in template.Campos.OrderBy(c => c.Orden))
            {
                var inferred = engine.Infer(field.ReglaMapeo, form, customer);

                // Lo que la persona ya cargó en el formulario gana sobre la inferencia: es una
                // decisión humana explícita y no se pisa.
                var hasManual = manual.TryGetValue(field.Clave, out var manualValue)
                    && !string.IsNullOrWhiteSpace(manualValue);

                document.Valores.Add(new ValorCampo
                {
                    Id = Guid.NewGuid(),
                    DocumentoExportacionId = document.Id,
                    CampoPlantillaId = field.Id,
                    Valor = hasManual ? manualValue : inferred?.Valor,
                    Origen = hasManual ? OrigenValor.Manual : OrigenValor.Inferido,
                    Confirmado = false,
                    InferidoDesde = hasManual ? null : inferred?.InferidoDesde,
                });
            }

            generated.Add(document);
        }

        await documentos.ReplaceForFormAsync(form.Id, generated, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                command.Actor,
                AuditActions.DocumentGenerated,
                AuditEntityTypes.Form,
                form.Id.ToString(),
                $"{generated.Count} documentos de {form.Code}."),
            cancellationToken);

        var byId = templates.ToDictionary(t => t.Id, t => t.Codigo);

        return Result.Success<IReadOnlyList<GeneratedDocumentDto>>(
        [
            .. generated.Select(d => new GeneratedDocumentDto(
                d.Id,
                form.Id,
                byId[d.PlantillaDocumentoId],
                DocumentStatuses.Generated,
                d.CreatedAt,
                d.CreatedByUserId)),
        ]);
    }
}
