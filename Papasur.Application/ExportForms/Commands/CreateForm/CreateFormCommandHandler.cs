using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.ExportForms.Ports;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Domain.Audit;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms.Commands.CreateForm;

public sealed class CreateFormCommandHandler(
    IExportFormRepository forms,
    FormItemBuilder itemBuilder,
    FormAssembler assembler,
    IAuditRepository audit)
    : ICommandHandler<CreateFormCommand, Result<ExportFormDto>>
{
    public async Task<Result<ExportFormDto>> Handle(CreateFormCommand command, CancellationToken cancellationToken)
    {
        var fields = command.Fields;

        var incoterm = fields.Incoterm ?? Incoterms.Fob;

        if (!Incoterms.Exists(incoterm))
        {
            return Result.Failure<ExportFormDto>(new Error("Form.IncotermInvalid", "El incoterm no es válido."));
        }

        var currency = fields.Currency ?? Currencies.Usd;

        if (!Currencies.Exists(currency))
        {
            return Result.Failure<ExportFormDto>(new Error("Form.CurrencyInvalid", "La moneda no es válida."));
        }

        var items = await itemBuilder.BuildAsync(fields.Items ?? [], cancellationToken);

        if (items.IsFailure)
        {
            return Result.Failure<ExportFormDto>(items.Error);
        }

        var now = DateTime.UtcNow;

        var form = new ExportForm
        {
            Id = Guid.NewGuid(),
            Code = await forms.NextCodeAsync(now.Year, cancellationToken),
            Status = FormStatuses.Draft,
            Version = 1,
            CustomerId = fields.CustomerId,
            DestinationCountryCode = fields.DestinationCountryCode ?? string.Empty,
            PortOfLoading = fields.PortOfLoading ?? string.Empty,
            PortOfDischarge = fields.PortOfDischarge ?? string.Empty,
            Incoterm = incoterm,
            Currency = currency,
            PaymentTerms = fields.PaymentTerms,
            ValidUntil = fields.ValidUntil,
            Notes = fields.Notes,
            RequirementValues = FormAssembler.WriteRequirementValues(fields.RequirementValues),
            CreatedByUserId = command.Actor.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Items = items.Value,
        };

        await forms.AddAsync(form, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                command.Actor,
                AuditActions.FormCreated,
                AuditEntityTypes.Form,
                form.Id.ToString(),
                form.Code),
            cancellationToken);

        return Result.Success(await assembler.ToDtoAsync(form, cancellationToken));
    }
}
