using Microsoft.EntityFrameworkCore;
using Papasur.Application.Abstractions;
using Papasur.Application.Lots.Ports;
using Papasur.Application.Lots.Queries.GetLots;
using Papasur.Domain.ExportForms;
using Papasur.Domain.Trazabilidad;
using Papasur.Infrastructure.Persistence;

namespace Papasur.Infrastructure.Lots;

/// <summary>
/// Proyecta la trazabilidad real al shape que consume el front (contrato §3).
///
/// El saldo NO está guardado: se calcula sumando los movimientos de la planilla. Entradas y
/// salidas son las etapas de la cadena real (ingreso a tolva, campo→frío y envío a frío entran;
/// retiro de frío y entrega a cliente salen).
///
/// Todo —incluido el filtro por el estado derivado— se resuelve en SQL, para que la paginación
/// siga siendo de base de datos (ai.md §11). Por eso las sumas están escritas a mano en cada
/// lugar: una expresión compartida no se traduce, y traer los lotes a memoria sería peor.
/// </summary>
public class EfLotProjectionRepository(AppDbContext db) : ILotProjectionRepository
{
    /// <summary>Nombre botánico de la papa: es lo que exige el certificado fitosanitario.</summary>
    private const string Species = "Solanum tuberosum";

    private static readonly string[] Inbound =
        [TiposMovimiento.IngresoTolva, TiposMovimiento.CampoAFrio, TiposMovimiento.EnvioFrio];

    private static readonly string[] Outbound =
        [TiposMovimiento.RetiroFrio, TiposMovimiento.EntregaCliente];

    /// <summary>Estados de formulario que comprometen stock: mientras vivan, los kilos están tomados.</summary>
    private static readonly string[] CommittingStatuses =
    [
        FormStatuses.Draft, FormStatuses.ChangesRequested, FormStatuses.Submitted, FormStatuses.Approved,
    ];

    public async Task<PagedResult<SeedLotDto>> ListAsync(
        PageRequest page,
        LotFilter filter,
        CancellationToken cancellationToken)
    {
        var query = db.Lotes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // Contrato §3: busca en código y variedad (la especie es una sola en todo el padrón).
            var pattern = $"%{filter.Search}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.Codigo, pattern)
                || EF.Functions.ILike(l.Variedad.Nombre, pattern));
        }

        if (filter.LocationId is { } locationId)
        {
            query = query.Where(l => l.StorageLocationId == locationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(l => l.Categoria == filter.Category);
        }

        query = ApplyStatusFilter(query, filter.Status);

        var total = await query.CountAsync(cancellationToken);

        var items = await ProjectAsync(
            query.OrderBy(l => l.Codigo).ThenBy(l => l.Id).Skip(page.Skip).Take(page.PageSize),
            cancellationToken);

        return new PagedResult<SeedLotDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<SeedLotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => (await ProjectAsync(db.Lotes.AsNoTracking().Where(l => l.Id == id), cancellationToken))
            .FirstOrDefault();

    public async Task<IReadOnlyList<SeedLotDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await ProjectAsync(db.Lotes.AsNoTracking().Where(l => ids.Contains(l.Id)), cancellationToken);
    }

    /// <summary>El estado es derivado, así que el filtro se traduce a la condición que lo produce.</summary>
    private IQueryable<Lote> ApplyStatusFilter(IQueryable<Lote> query, string? status) => status switch
    {
        null or "" => query,

        LotStatuses.Quarantined => query.Where(l => l.EnCuarentena),

        LotStatuses.Depleted => query.Where(l =>
            !l.EnCuarentena
            && (l.Movimientos.Where(m => Inbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m)
                - (l.Movimientos.Where(m => Outbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m) <= 0m),

        LotStatuses.Reserved => query.Where(l =>
            !l.EnCuarentena
            && (l.Movimientos.Where(m => Inbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m)
                - (l.Movimientos.Where(m => Outbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m) > 0m
            && (db.ExportFormItems
                .Where(i => i.LotId == l.Id && CommittingStatuses.Contains(i.ExportForm.Status))
                .Sum(i => (decimal?)i.QuantityKg) ?? 0m) > 0m),

        LotStatuses.Available => query.Where(l =>
            !l.EnCuarentena
            && (l.Movimientos.Where(m => Inbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m)
                - (l.Movimientos.Where(m => Outbound.Contains(m.Tipo)).Sum(m => (decimal?)m.Kilogramos) ?? 0m) > 0m
            && (db.ExportFormItems
                .Where(i => i.LotId == l.Id && CommittingStatuses.Contains(i.ExportForm.Status))
                .Sum(i => (decimal?)i.QuantityKg) ?? 0m) <= 0m),

        // Un estado desconocido no devuelve todo: devuelve nada, que es lo honesto.
        _ => query.Where(l => false),
    };

    /// <summary>
    /// Materializa la página y proyecta en memoria.
    ///
    /// El filtrado, el orden y la paginación siguen resolviéndose en SQL (que es lo que importa
    /// para no traer la tabla entera); sólo la proyección se hace acá. Escribirla como Select
    /// traducible obligaba a repetir cada suma cuatro veces y terminaba sin traducirse igual
    /// —la navegación opcional a StorageLocation rompía la traducción y devolvía 500—.
    /// Son ~150 lotes: el costo es irrelevante y desaparece toda una familia de errores.
    /// </summary>
    private async Task<List<SeedLotDto>> ProjectAsync(
        IQueryable<Lote> lotes,
        CancellationToken cancellationToken)
    {
        var entidades = await lotes
            .Include(l => l.Variedad)
            .Include(l => l.StorageLocation)
            .Include(l => l.Movimientos)
            .ToListAsync(cancellationToken);

        if (entidades.Count == 0)
        {
            return [];
        }

        var ids = entidades.Select(l => l.Id).ToList();

        // Kilos comprometidos por formularios vivos, en una sola consulta.
        var comprometidos = await db.ExportFormItems
            .AsNoTracking()
            .Where(i => ids.Contains(i.LotId) && CommittingStatuses.Contains(i.ExportForm.Status))
            .GroupBy(i => i.LotId)
            .Select(g => new { LotId = g.Key, Kg = g.Sum(i => (decimal?)i.QuantityKg) ?? 0m })
            .ToDictionaryAsync(x => x.LotId, x => x.Kg, cancellationToken);

        return [.. entidades.Select(l => ToDto(l, comprometidos.GetValueOrDefault(l.Id)))];
    }

    private static SeedLotDto ToDto(Lote l, decimal reservados)
    {
        var entradas = l.Movimientos.Where(m => Inbound.Contains(m.Tipo)).Sum(m => m.Kilogramos);
        var salidas = l.Movimientos.Where(m => Outbound.Contains(m.Tipo)).Sum(m => m.Kilogramos);
        var saldo = entradas - salidas;
        var disponible = saldo > 0m ? saldo : 0m;

        var status = l.EnCuarentena
            ? LotStatuses.Quarantined
            : saldo <= 0m
                ? LotStatuses.Depleted
                : reservados > 0m
                    ? LotStatuses.Reserved
                    : LotStatuses.Available;

        return new SeedLotDto(
            l.Id,
            l.Codigo,
            Species,
            l.Variedad?.Nombre ?? string.Empty,
            l.Categoria ?? string.Empty,
            l.Campania ?? l.Movimientos.Select(m => (DateTime?)m.Fecha).Min()?.Year ?? l.CreatedAt.Year,
            l.StorageLocationId,
            l.StorageLocation?.Code ?? string.Empty,
            l.Posicion,
            entradas,
            disponible,
            reservados,
            l.PoderGerminativo,
            l.Pureza,
            l.Humedad,
            l.Tratamiento,
            l.RegistroInase,
            status,
            l.Movimientos.Select(m => (DateTime?)m.Fecha).Max(),
            null);
    }
}
