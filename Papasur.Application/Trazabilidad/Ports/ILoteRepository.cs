using Papasur.Application.Abstractions;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Trazabilidad.Ports;

/// <summary>
/// Puerto de lectura de lotes y su trazabilidad. Implementado en Infrastructure (EfLoteRepository).
/// </summary>
public interface ILoteRepository
{
    /// <summary>Listado paginado, opcionalmente filtrado por variedad o texto (código de lote). Incluye Variedad y Campo.</summary>
    Task<PagedResult<Lote>> ListAsync(
        PageRequest page,
        string? search,
        Guid? variedadId,
        CancellationToken cancellationToken);

    /// <summary>Trae el lote con Variedad, Campo y sus Movimientos (con Transportista y Cliente) cargados.</summary>
    Task<Lote?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
