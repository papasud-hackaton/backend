using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Queries.GetFormById;

namespace Papasur.Application.ExportForms.Commands.UpdateForm;

/// <summary>
/// Edición con bloqueo optimista (contrato §0.1). IfMatch null = cliente que no lo maneja: pasa
/// igual. El front siempre lo manda.
/// </summary>
public sealed record UpdateFormCommand(Guid Id, FormFieldsInput Fields, int? IfMatch, Actor Actor)
    : ICommand<Result<UpdateFormResult>>;

/// <summary>
/// Resultado de una edición: o quedó guardada, o hubo conflicto de versión y se devuelve el
/// estado actual para que el front pueda mostrar qué cambió.
/// </summary>
public sealed record UpdateFormResult(ExportFormDto Form, int? ConflictVersion)
{
    public bool IsConflict => ConflictVersion is not null;

    public static UpdateFormResult Updated(ExportFormDto form) => new(form, null);

    public static UpdateFormResult Conflict(ExportFormDto current) => new(current, current.Version);
}
