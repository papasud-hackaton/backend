using Papasur.Application.Abstractions;
using Papasur.Application.Lots.Queries.GetLots;

namespace Papasur.Application.Lots.Ports;

/// <summary>Filtros del listado de lotes (contrato §3).</summary>
public sealed record LotFilter(
    string? Search = null,
    Guid? LocationId = null,
    string? Category = null,
    string? Status = null);

/// <summary>
/// Lado de lectura de los lotes en el shape del contrato. Está separado de ILoteRepository a
/// propósito: aquél devuelve la entidad para el copiloto, éste devuelve el saldo calculado.
/// </summary>
public interface ILotProjectionRepository
{
    Task<PagedResult<SeedLotDto>> ListAsync(PageRequest page, LotFilter filter, CancellationToken cancellationToken);

    Task<SeedLotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Varios de una: lo que necesita el alta de líneas para congelar la trazabilidad.</summary>
    Task<IReadOnlyList<SeedLotDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
