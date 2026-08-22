using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.ExportForms.Ports;
using Papasur.Domain.ExportForms;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.ExportForms;

public class EfExportFormRepository(AppDbContext db) : IExportFormRepository
{
    public async Task<PagedResult<ExportForm>> ListAsync(
        PageRequest page,
        FormFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.ExportForms
            .AsNoTracking()
            .Include(f => f.Items)
            .Include(f => f.Customer)
            .Include(f => f.CreatedByUser)
            .AsQueryable();

        if (filter.CreatedBy is { } createdBy)
        {
            query = query.Where(f => f.CreatedByUserId == createdBy);
        }

        if (filter.Statuses is { Count: > 0 } statuses)
        {
            query = query.Where(f => statuses.Contains(f.Status));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // Contrato §5: busca en código y nombre de cliente.
            var pattern = $"%{filter.Search}%";
            query = query.Where(f =>
                EF.Functions.ILike(f.Code, pattern)
                || (f.Customer != null && EF.Functions.ILike(f.Customer.Nombre, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);

        var forms = await query
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ExportForm>(forms, page.Page, page.PageSize, total);
    }

    // Tracked a propósito: el mismo grafo se lee, se edita y se guarda.
    public async Task<ExportForm?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.ExportForms
            .Include(f => f.Items)
            .Include(f => f.Customer)
            .Include(f => f.CreatedByUser)
            .Include(f => f.Documents).ThenInclude(d => d.PlantillaDocumento)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task AddAsync(ExportForm form, CancellationToken cancellationToken)
    {
        db.ExportForms.Add(form);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ExportForm form,
        IReadOnlyList<ExportFormItem>? replacementItems,
        CancellationToken cancellationToken)
    {
        if (replacementItems is not null)
        {
            // Por el DbSet y no vaciando la colección: la trazabilidad congelada es un tipo de
            // propiedad en la misma tabla, y limpiar la navegación hace que EF intente borrar la
            // fila dos veces.
            db.ExportFormItems.RemoveRange([.. form.Items]);
            form.Items.Clear();

            foreach (var item in replacementItems)
            {
                item.ExportFormId = form.Id;
                // Sólo al DbSet: EF hace el fixup de la navegación. Agregarlo también a
                // form.Items lo dejaría duplicado en la respuesta de esta misma llamada.
                db.ExportFormItems.Add(item);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> NextCodeAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"PF-{year}-";

        var last = await db.ExportForms
            .AsNoTracking()
            .Where(f => f.Code.StartsWith(prefix))
            .OrderByDescending(f => f.Code)
            .Select(f => f.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var next = last is not null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;

        return $"{prefix}{next:D4}";
    }

    public async Task<IReadOnlyList<ExportForm>> ListAllForMetricsAsync(
        Guid? createdBy,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = db.ExportForms
            .AsNoTracking()
            .Include(f => f.Items)
            .Include(f => f.Customer)
            .Include(f => f.CreatedByUser)
            .AsQueryable();

        if (createdBy is { } userId)
        {
            query = query.Where(f => f.CreatedByUserId == userId);
        }

        if (from is { } desde)
        {
            query = query.Where(f => f.CreatedAt >= desde);
        }

        if (to is { } hasta)
        {
            // El "hasta" del usuario es un día completo, no un instante.
            var end = hasta.Date.AddDays(1);
            query = query.Where(f => f.CreatedAt < end);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
