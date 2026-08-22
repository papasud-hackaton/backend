using Papasur.Application.Abstractions;
using Papasur.Domain.Documentos;

namespace Papasur.Application.Documentos.Ports;

/// <summary>
/// Puerto de lectura de plantillas documentales (requisitos como dato). Implementado en Infrastructure.
/// </summary>
public interface IPlantillaRepository
{
    /// <summary>Listado paginado; con soloActivas = true devuelve sólo las que se pueden usar para generar.</summary>
    Task<PagedResult<PlantillaDocumento>> ListAsync(PageRequest page, bool soloActivas, CancellationToken cancellationToken);

    /// <summary>Trae la plantilla con sus campos (CampoPlantilla) cargados y ordenados.</summary>
    Task<PlantillaDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Plantillas activas de un ámbito, con sus campos ordenados. Sin paginar: son los requisitos
    /// documentales completos, y el contrato §4 los devuelve como array.
    /// </summary>
    Task<IReadOnlyList<PlantillaDocumento>> ListByAmbitoAsync(string ambito, CancellationToken cancellationToken);
}
