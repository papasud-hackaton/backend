using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotePorId;
using Papasur.Application.Trazabilidad.Queries.ObtenerLotes;

namespace Papasur.Api.Controllers;

/// <summary>
/// Lotes de papa semilla y su trazabilidad. Es la selección rápida sobre la que se generan los
/// documentos de exportación. Sólo lectura (la trazabilidad se importa desde la planilla).
/// </summary>
[ApiController]
[Route("api/v1/lotes")]
[Authorize]
public class LotesController : ControllerBase
{
    /// <summary>Listado paginado de lotes, filtrable por variedad o texto (código/variedad).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<LoteDto>>> List(
        [FromServices] IQueryHandler<ObtenerLotesQuery, PagedResult<LoteDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] Guid? variedadId = null)
    {
        var result = await handler.Handle(
            new ObtenerLotesQuery(new PageRequest(page, pageSize), search, variedadId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Detalle de un lote con todos sus movimientos (trazabilidad completa).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoteDetalleDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<ObtenerLotePorIdQuery, Result<LoteDetalleDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ObtenerLotePorIdQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }
}
