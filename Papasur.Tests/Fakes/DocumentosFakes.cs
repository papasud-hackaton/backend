using Papasur.Application.Abstractions;
using Papasur.Application.Documentos.Ports;
using Papasur.Application.Trazabilidad.Ports;
using Papasur.Domain.Documentos;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Tests.Fakes;

public sealed class FakeLoteRepository : ILoteRepository
{
    public List<Lote> Lotes { get; } = [];

    public Task<PagedResult<Lote>> ListAsync(
        PageRequest page,
        string? search,
        Guid? variedadId,
        CancellationToken cancellationToken)
    {
        var query = Lotes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.Codigo.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (variedadId is { } v)
        {
            query = query.Where(l => l.VariedadId == v);
        }

        var all = query.ToList();

        return Task.FromResult(new PagedResult<Lote>(
            all.Skip(page.Skip).Take(page.PageSize).ToList(),
            page.Page,
            page.PageSize,
            all.Count));
    }

    public Task<Lote?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Lotes.FirstOrDefault(l => l.Id == id));

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Lotes.Any(l => l.Id == id));
}

public sealed class FakePlantillaRepository : IPlantillaRepository
{
    public List<PlantillaDocumento> Plantillas { get; } = [];

    public Task<PagedResult<PlantillaDocumento>> ListAsync(
        PageRequest page,
        bool soloActivas,
        CancellationToken cancellationToken)
    {
        var all = Plantillas.Where(p => !soloActivas || p.Activa).ToList();

        return Task.FromResult(new PagedResult<PlantillaDocumento>(
            all.Skip(page.Skip).Take(page.PageSize).ToList(),
            page.Page,
            page.PageSize,
            all.Count));
    }

    public Task<PlantillaDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Plantillas.FirstOrDefault(p => p.Id == id));
}

public sealed class FakeDocumentoRepository : IDocumentoRepository
{
    public List<DocumentoExportacion> Documentos { get; } = [];

    public int Updates { get; private set; }

    public Task AddAsync(DocumentoExportacion documento, CancellationToken cancellationToken)
    {
        Documentos.Add(documento);
        return Task.CompletedTask;
    }

    public Task<DocumentoExportacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Documentos.FirstOrDefault(d => d.Id == id));

    public Task UpdateAsync(DocumentoExportacion documento, CancellationToken cancellationToken)
    {
        Updates++;
        return Task.CompletedTask;
    }
}
