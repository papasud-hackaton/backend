using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Trazabilidad.Ports;
using Papasur.Domain.Trazabilidad;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Trazabilidad;

public class EfLoteRepository(AppDbContext db) : ILoteRepository
{
    public async Task<PagedResult<Lote>> ListAsync(
        PageRequest page,
        string? search,
        Guid? variedadId,
        CancellationToken cancellationToken)
    {
        var query = db.Lotes
            .AsNoTracking()
            .Include(l => l.Variedad)
            .Include(l => l.Campo)
            .Include(l => l.Movimientos)
            .AsQueryable();

        if (variedadId is { } vid)
        {
            query = query.Where(l => l.VariedadId == vid);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l =>
                EF.Functions.ILike(l.Codigo, $"%{search}%")
                || EF.Functions.ILike(l.Variedad.Nombre, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var lotes = await query
            .OrderBy(l => l.Codigo)
            .ThenBy(l => l.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Lote>(lotes, page.Page, page.PageSize, total);
    }

    public async Task<Lote?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Lotes
            .AsNoTracking()
            .Include(l => l.Variedad)
            .Include(l => l.Campo)
            .Include(l => l.Movimientos).ThenInclude(m => m.Transportista)
            .Include(l => l.Movimientos).ThenInclude(m => m.Cliente)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => db.Lotes.AnyAsync(l => l.Id == id, cancellationToken);
}
