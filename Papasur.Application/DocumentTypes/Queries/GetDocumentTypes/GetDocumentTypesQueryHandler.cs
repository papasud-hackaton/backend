using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Documentos.Ports;
using Papasur.Domain.Documentos;

namespace Papasur.Application.DocumentTypes.Queries.GetDocumentTypes;

public sealed class GetDocumentTypesQueryHandler(IPlantillaRepository plantillas)
    : IQueryHandler<GetDocumentTypesQuery, Result<IReadOnlyList<DocumentTypeDto>>>
{
    public async Task<Result<IReadOnlyList<DocumentTypeDto>>> Handle(
        GetDocumentTypesQuery query,
        CancellationToken cancellationToken)
    {
        var templates = await plantillas.ListByAmbitoAsync(AmbitosPlantilla.Formulario, cancellationToken);

        return Result.Success<IReadOnlyList<DocumentTypeDto>>(
        [
            .. templates.Select(t => new DocumentTypeDto(
                t.Codigo,
                t.Nombre,
                t.Organismo,
                "always",
                [
                    .. t.Campos.OrderBy(c => c.Orden).Select(c => new RequirementFieldDto(
                        c.Clave,
                        c.Etiqueta,
                        c.Origen,
                        c.Obligatorio,
                        c.ReglaMapeo,
                        c.Ayuda)),
                ])),
        ]);
    }
}
