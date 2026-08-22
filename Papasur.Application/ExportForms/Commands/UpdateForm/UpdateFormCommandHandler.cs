using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.ExportForms.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms.Commands.UpdateForm;

public sealed class UpdateFormCommandHandler(
    IExportFormRepository forms,
    FormItemBuilder itemBuilder,
    FormAssembler assembler,
    IAuditRepository audit)
    : ICommandHandler<UpdateFormCommand, Result<UpdateFormResult>>
{
    /// <summary>Error que el controller mapea a 403; el mensaje se muestra tal cual.</summary>
    public static readonly Error NotEditable =
        new("Form.NotEditable", "Este formulario no se puede editar en su estado actual.");

    public async Task<Result<UpdateFormResult>> Handle(UpdateFormCommand command, CancellationToken cancellationToken)
    {
        var form = await forms.GetByIdAsync(command.Id, cancellationToken);

        if (form is null)
        {
            return Result.Failure<UpdateFormResult>(new Error("Form.NotFound", "Formulario no encontrado."));
        }

        if (!FormStateMachine.IsEditable(form.Status, form.CreatedByUserId, command.Actor.Id, command.Actor.Role))
        {
            return Result.Failure<UpdateFormResult>(NotEditable);
        }

        // Bloqueo optimista: si la versión no coincide no se escribe NADA y se devuelve el estado
        // actual. Es el dolor original del cliente (la planilla compartida), no una formalidad.
        if (command.IfMatch is { } expected && expected != form.Version)
        {
            return Result.Success(UpdateFormResult.Conflict(await assembler.ToDtoAsync(form, cancellationToken)));
        }

        var fields = command.Fields;

        if (fields.Incoterm is { } incoterm)
        {
            if (!Incoterms.Exists(incoterm))
            {
                return Result.Failure<UpdateFormResult>(new Error("Form.IncotermInvalid", "El incoterm no es válido."));
            }

            form.Incoterm = incoterm;
        }

        if (fields.Currency is { } currency)
        {
            if (!Currencies.Exists(currency))
            {
                return Result.Failure<UpdateFormResult>(new Error("Form.CurrencyInvalid", "La moneda no es válida."));
            }

            form.Currency = currency;
        }

        List<Domain.ExportForms.ExportFormItem>? replacementItems = null;

        if (fields.Items is { } inputs)
        {
            var items = await itemBuilder.BuildAsync(inputs, cancellationToken);

            if (items.IsFailure)
            {
                return Result.Failure<UpdateFormResult>(items.Error);
            }

            replacementItems = items.Value;
        }

        if (fields.CustomerId is { } customerId)
        {
            form.CustomerId = customerId;
        }

        if (fields.DestinationCountryCode is { } country)
        {
            form.DestinationCountryCode = country;
        }

        if (fields.PortOfLoading is { } loading)
        {
            form.PortOfLoading = loading;
        }

        if (fields.PortOfDischarge is { } discharge)
        {
            form.PortOfDischarge = discharge;
        }

        if (fields.PaymentTerms is { } paymentTerms)
        {
            form.PaymentTerms = paymentTerms;
        }

        if (fields.ValidUntil is { } validUntil)
        {
            form.ValidUntil = validUntil;
        }

        if (fields.Notes is { } notes)
        {
            form.Notes = notes;
        }

        if (fields.RequirementValues is { } requirementValues)
        {
            form.RequirementValues = FormAssembler.WriteRequirementValues(requirementValues);
        }

        form.Version += 1;
        form.UpdatedAt = DateTime.UtcNow;

        try
        {
            await forms.UpdateAsync(form, replacementItems, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Alguien más guardó primero: se devuelve el estado actual para que
            // el front pueda mostrar qué cambió (contrato §0.1).
            var actual = await forms.GetByIdAsync(command.Id, cancellationToken);
            return Result.Success(UpdateFormResult.Conflict(
                await assembler.ToDtoAsync(actual!, cancellationToken)));
        }

        await audit.AddAsync(
            AuditFactory.Create(
                command.Actor,
                AuditActions.FormUpdated,
                AuditEntityTypes.Form,
                form.Id.ToString(),
                form.Code,
                AuditFactory.ChangeSet(("version", (form.Version - 1).ToString(), form.Version.ToString()))),
            cancellationToken);

        return Result.Success(UpdateFormResult.Updated(await assembler.ToDtoAsync(form, cancellationToken)));
    }
}
