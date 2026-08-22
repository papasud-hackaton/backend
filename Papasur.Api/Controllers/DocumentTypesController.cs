using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.DocumentTypes.Queries.GetDocumentTypes;

namespace Papasur.Api.Controllers;

/// <summary>
/// Requisitos documentales (contrato §4). Salen de plantilla_documento + campo_plantilla:
/// cambiar plantillas es cambiar DATOS, no código ni un redeploy.
/// </summary>
[Route("api/v1/document-types")]
public class DocumentTypesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeDto>>> List(
        [FromServices] IQueryHandler<GetDocumentTypesQuery, Result<IReadOnlyList<DocumentTypeDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetDocumentTypesQuery(), cancellationToken);

        return Ok(result.Value);
    }
}
