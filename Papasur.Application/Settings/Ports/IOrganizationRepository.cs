using Papasur.Domain.Settings;

namespace Papasur.Application.Settings.Ports;

public interface IOrganizationRepository
{
    /// <summary>Devuelve la fila única, creándola vacía si todavía no existe.</summary>
    Task<OrganizationSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(OrganizationSettings settings, CancellationToken cancellationToken);
}
