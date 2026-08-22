using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Queries.GetFormById;

namespace Papasur.Application.ExportForms.Commands.GenerateFormDocuments;

/// <summary>Genera los documentos del envío (contrato §5). Regenerar rehace el juego completo.</summary>
public sealed record GenerateFormDocumentsCommand(Guid FormId, Actor Actor)
    : ICommand<Result<IReadOnlyList<GeneratedDocumentDto>>>;
