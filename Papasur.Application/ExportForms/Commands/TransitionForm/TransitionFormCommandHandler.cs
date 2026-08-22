using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.ExportForms.Ports;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Domain.Audit;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms.Commands.TransitionForm;

public sealed class TransitionFormCommandHandler(
    IExportFormRepository forms,
    FormAssembler assembler,
    IAuditRepository audit)
    : ICommandHandler<TransitionFormCommand, Result<ExportFormDto>>
{
    /// <summary>Prefijo de los errores que el controller mapea a 403 en vez de 400.</summary>
    public const string NotAllowedCode = "Form.TransitionNotAllowed";

    public async Task<Result<ExportFormDto>> Handle(TransitionFormCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Action) || !FormActions.Exists(command.Action))
        {
            return Result.Failure<ExportFormDto>(new Error("Form.ActionRequired", "Falta la acción."));
        }

        var form = await forms.GetByIdAsync(command.Id, cancellationToken);

        if (form is null)
        {
            return Result.Failure<ExportFormDto>(new Error("Form.NotFound", "Formulario no encontrado."));
        }

        var warnings = await assembler.WarningsAsync(form, cancellationToken);

        var view = new FormView(
            form.Status,
            form.CreatedByUserId,
            form.Items.Count,
            FormCalculations.HasBlocking(warnings),
            form.Documents.Count);

        var check = FormStateMachine.CanTransition(view, command.Action, command.Actor.Id, command.Actor.Role);

        if (!check.Allowed)
        {
            // El 403 lleva el motivo REAL: el front lo muestra tal cual.
            return Result.Failure<ExportFormDto>(new Error(NotAllowedCode, check.Reason));
        }

        var rule = FormStateMachine.Transitions[command.Action];

        if (rule.Requires == FormStateMachine.RequiresReviewNotes && string.IsNullOrWhiteSpace(command.ReviewNotes))
        {
            return Result.Failure<ExportFormDto>(
                new Error("Form.ReviewNotesRequired", "Las notas de revisión son obligatorias."));
        }

        if (rule.Requires == FormStateMachine.RequiresReason && string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure<ExportFormDto>(new Error("Form.ReasonRequired", "El motivo es obligatorio."));
        }

        var from = form.Status;
        var now = DateTime.UtcNow;

        form.Status = FormStateMachine.NextStatus(command.Action);
        form.UpdatedAt = now;

        switch (command.Action)
        {
            case FormActions.Submit:
                form.SubmittedAt = now;
                form.ReviewNotes = null;
                break;

            case FormActions.Approve:
                form.ReviewedByUserId = command.Actor.Id;
                form.ReviewedAt = now;
                form.ReviewNotes = null;
                break;

            case FormActions.RequestChanges:
                form.ReviewedByUserId = command.Actor.Id;
                form.ReviewedAt = now;
                form.ReviewNotes = command.ReviewNotes;
                break;

            case FormActions.Issue:
                form.IssuedAt = now;
                break;

            case FormActions.Reopen:
                // Reabrir es una escritura: sube la versión para que nadie siga editando a ciegas.
                form.Version += 1;
                break;
        }

        await forms.UpdateAsync(form, null, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                command.Actor,
                FormStateMachine.AuditActionFor(command.Action),
                AuditEntityTypes.Form,
                form.Id.ToString(),
                command.Reason ?? command.ReviewNotes ?? form.Code,
                AuditFactory.ChangeSet(("status", from, form.Status))),
            cancellationToken);

        return Result.Success(await assembler.ToDtoAsync(form, cancellationToken));
    }
}
