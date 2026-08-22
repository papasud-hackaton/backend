using Microsoft.EntityFrameworkCore;
using Papasur.Application.Documentos.Ports;
using Papasur.Domain.Documentos;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Documentos;

public class EfDocumentoRepository(AppDbContext db) : IDocumentoRepository
{
    public async Task AddAsync(DocumentoExportacion documento, CancellationToken cancellationToken)
    {
        db.DocumentosExportacion.Add(documento);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Tracked a propósito: el mismo grafo se usa para leer y para confirmar (UpdateAsync = SaveChanges).
    public async Task<DocumentoExportacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.DocumentosExportacion
            .Include(d => d.Lote!).ThenInclude(l => l.Variedad)
            .Include(d => d.Movimiento)
            .Include(d => d.PlantillaDocumento)
            .Include(d => d.Status)
            .Include(d => d.Valores).ThenInclude(v => v.CampoPlantilla)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task UpdateAsync(DocumentoExportacion documento, CancellationToken cancellationToken)
        => await db.SaveChangesAsync(cancellationToken);

    public async Task ReplaceForFormAsync(
        Guid exportFormId,
        IReadOnlyList<DocumentoExportacion> documentos,
        CancellationToken cancellationToken)
    {
        // Los valores caen con el documento (cascade); regenerar deja un solo juego vigente.
        var previos = await db.DocumentosExportacion
            .Where(d => d.ExportFormId == exportFormId)
            .ToListAsync(cancellationToken);

        db.DocumentosExportacion.RemoveRange(previos);
        db.DocumentosExportacion.AddRange(documentos);

        await db.SaveChangesAsync(cancellationToken);
    }
}
