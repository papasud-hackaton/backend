using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Queries.GetFormById;

namespace Papasur.Application.ExportForms.Commands.TransitionForm;

/// <summary>La máquina de estados (contrato §5). El único camino por el que cambia el status.</summary>
public sealed record TransitionFormCommand(
    Guid Id,
    string Action,
    string? ReviewNotes,
    string? Reason,
    Actor Actor) : ICommand<Result<ExportFormDto>>;
