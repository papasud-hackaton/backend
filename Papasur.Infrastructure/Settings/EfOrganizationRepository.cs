using Microsoft.EntityFrameworkCore;
using Papasur.Application.Settings.Ports;
using Papasur.Domain.Settings;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Settings;

public class EfOrganizationRepository(AppDbContext db) : IOrganizationRepository
{
    public async Task<OrganizationSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await db.OrganizationSettings
            .FirstOrDefaultAsync(o => o.Id == OrganizationSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        // Primera lectura: se crea la fila vacía para que el PATCH siempre tenga sobre qué escribir.
        settings = new OrganizationSettings { UpdatedAt = DateTime.UtcNow };
        db.OrganizationSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public async Task SaveAsync(OrganizationSettings settings, CancellationToken cancellationToken)
    {
        db.OrganizationSettings.Update(settings);
        await db.SaveChangesAsync(cancellationToken);
    }
}
