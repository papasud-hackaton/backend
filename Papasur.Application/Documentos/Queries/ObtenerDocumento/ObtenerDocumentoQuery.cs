using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Documentos.Queries.ObtenerDocumento;

public sealed record ObtenerDocumentoQuery(Guid Id) : IQuery<Result<DocumentoExportacionDto>>;
