using Papasur.Application.Abstractions;
using Papasur.Domain.ExportForms;

namespace Papasur.Application.ExportForms.Ports;

/// <summary>
/// Filtros del listado (contrato §5). Statuses es repetible: ?status=draft&amp;status=submitted.
/// OnlyCreatedBy lo impone el servidor para un agente, sin importar qué mande el cliente.
/// </summary>
public sealed record FormFilter(
    IReadOnlyList<string>? Statuses = null,
    Guid? CreatedBy = null,
    string? Search = null);

/// <summary>Puerto del agregado ExportForm. Trae siempre las líneas: el formulario no existe sin ellas.</summary>
public interface IExportFormRepository
{
    Task<PagedResult<ExportForm>> ListAsync(PageRequest page, FormFilter filter, CancellationToken cancellationToken);

    Task<ExportForm?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ExportForm form, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda el formulario. <paramref name="replacementItems"/> null deja las líneas como están;
    /// con valor, las reemplaza por completo. Va junto porque es UNA transacción: media edición
    /// guardada es peor que ninguna.
    /// </summary>
    Task UpdateAsync(
        ExportForm form,
        IReadOnlyList<ExportFormItem>? replacementItems,
        CancellationToken cancellationToken);

    /// <summary>Correlativo por año: PF-2026-0061. Lo asigna el servidor, nunca el cliente.</summary>
    Task<string> NextCodeAsync(int year, CancellationToken cancellationToken);

    /// <summary>Kilos comprometidos por lote en formularios vivos: alimenta el reservedKg del lote.</summary>
    Task<IReadOnlyList<ExportForm>> ListAllForMetricsAsync(
        Guid? createdBy,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);
}
