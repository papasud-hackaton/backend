using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Documentos.Ports;

namespace Papasur.Application.Documentos.Queries.ObtenerPlantillas;

public sealed class ObtenerPlantillasQueryHandler(IPlantillaRepository plantillas)
    : IQueryHandler<ObtenerPlantillasQuery, PagedResult<PlantillaDto>>
{
    public async Task<PagedResult<PlantillaDto>> Handle(
        ObtenerPlantillasQuery query,
        CancellationToken cancellationToken)
    {
        var page = await plantillas.ListAsync(query.Page, query.SoloActivas, cancellationToken);

        return page.Map(p => new PlantillaDto(
            p.Id,
            p.Nombre,
            p.Tipo,
            p.Organismo,
            p.PaisDestino,
            p.Version,
            p.Activa,
            p.Campos.Count));
    }
}
