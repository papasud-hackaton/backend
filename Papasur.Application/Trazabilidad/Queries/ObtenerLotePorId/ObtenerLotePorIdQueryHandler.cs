using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Trazabilidad.Ports;

namespace Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;

public sealed class ObtenerLotePorIdQueryHandler(ILoteRepository lotes)
    : IQueryHandler<ObtenerLotePorIdQuery, Result<LoteDetalleDto>>
{
    public async Task<Result<LoteDetalleDto>> Handle(ObtenerLotePorIdQuery query, CancellationToken cancellationToken)
    {
        var lote = await lotes.GetByIdAsync(query.Id, cancellationToken);

        if (lote is null)
        {
            return Result.Failure<LoteDetalleDto>(new Error("Lote.NotFound", "El lote indicado no existe."));
        }

        var movimientos = lote.Movimientos
            .OrderBy(m => m.Fecha)
            .Select(m => new MovimientoDto(
                m.Id,
                m.Tipo,
                m.NumeroRemito,
                m.Fecha,
                m.Kilogramos,
                m.Bolsas,
                m.KgPromedio,
                m.Presentacion,
                m.Categoria,
                m.Calibre,
                m.Transportista?.Nombre,
                m.Cliente?.Nombre,
                m.Cliente?.Pais,
                m.Comisionista,
                m.Destino,
                m.Dtv,
                m.Observaciones))
            .ToList();

        return Result.Success(new LoteDetalleDto(
            lote.Id,
            lote.Codigo,
            lote.VariedadId,
            lote.Variedad?.Nombre ?? string.Empty,
            lote.CampoId,
            lote.Campo?.Nombre,
            lote.Campo?.Establecimiento,
            lote.Categoria,
            lote.SuperficieHa,
            lote.CreatedAt,
            movimientos));
    }
}
