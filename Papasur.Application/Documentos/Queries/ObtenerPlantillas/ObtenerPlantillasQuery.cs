using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Documentos.Queries.ObtenerPlantillas;

public sealed record ObtenerPlantillasQuery(PageRequest Page, bool SoloActivas)
    : IQuery<PagedResult<PlantillaDto>>;
