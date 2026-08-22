using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.DocumentTypes.Queries.GetDocumentTypes;

public sealed record GetDocumentTypesQuery : IQuery<Result<IReadOnlyList<DocumentTypeDto>>>;
