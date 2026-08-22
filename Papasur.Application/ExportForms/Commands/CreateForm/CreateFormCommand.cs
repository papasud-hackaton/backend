using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Queries.GetFormById;

namespace Papasur.Application.ExportForms.Commands.CreateForm;

/// <summary>
/// Crea el borrador (contrato §5). El servidor asigna id, code, status draft, version 1 y autor;
/// cualquier cosa que el cliente mande sobre esos campos se ignora.
/// </summary>
public sealed record CreateFormCommand(FormFieldsInput Fields, Actor Actor) : ICommand<Result<ExportFormDto>>;
