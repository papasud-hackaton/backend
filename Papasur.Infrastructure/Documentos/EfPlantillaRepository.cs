using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Documentos.Ports;
using Papasur.Domain.Documentos;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Documentos;

public class EfPlantillaRepository(AppDbContext db) : IPlantillaRepository
{
    public async Task<PagedResult<PlantillaDocumento>> ListAsync(
        PageRequest page,
        bool soloActivas,
        CancellationToken cancellationToken)
    {
        var query = db.PlantillasDocumento
            .AsNoTracking()
            .Include(p => p.Campos)
            .AsQueryable();

        if (soloActivas)
        {
            query = query.Where(p => p.Activa);
        }

        var total = await query.CountAsync(cancellationToken);

        var plantillas = await query
            .OrderBy(p => p.Nombre)
            .ThenByDescending(p => p.Version)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlantillaDocumento>(plantillas, page.Page, page.PageSize, total);
    }

    public async Task<PlantillaDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.PlantillasDocumento
            .AsNoTracking()
            .Include(p => p.Campos)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PlantillaDocumento>> ListByAmbitoAsync(
        string ambito,
        CancellationToken cancellationToken)
        => await db.PlantillasDocumento
            .AsNoTracking()
            .Include(p => p.Campos)
            .Where(p => p.Activa && p.Ambito == ambito)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
}
