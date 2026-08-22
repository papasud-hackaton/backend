using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Documentos.Commands.ConfirmarDocumento;
using Papasur.Application.Documentos.Commands.GenerarBorrador;
using Papasur.Application.Documentos.Queries.ObtenerDocumento;
using Papasur.Application.Documentos.Queries.ObtenerPlantillas;

namespace Papasur.Api.Controllers;

/// <summary>
/// Copiloto de documentación de exportación: genera borradores pre-completados por inferencia
/// sobre la trazabilidad de un lote y los confirma con revisión humana explícita.
/// </summary>
[ApiController]
[Route("api/v1/documentos")]
[Authorize]
public class DocumentosController : ControllerBase
{
    /// <summary>Plantillas documentales disponibles (requisitos como dato) para generar documentos.</summary>
    [HttpGet("plantillas")]
    public async Task<ActionResult<PagedResult<PlantillaDto>>> Plantillas(
        [FromServices] IQueryHandler<ObtenerPlantillasQuery, PagedResult<PlantillaDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool soloActivas = true)
    {
        return Ok(await handler.Handle(
            new ObtenerPlantillasQuery(new PageRequest(page, pageSize), soloActivas),
            cancellationToken));
    }

    /// <summary>Genera un borrador pre-completado por inferencia para un lote + plantilla.</summary>
    [HttpPost("generar")]
    public async Task<ActionResult<Guid>> Generar(
        [FromBody] GenerarBorradorRequest request,
        [FromServices] ICommandHandler<GenerarBorradorCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var command = new GenerarBorradorCommand(request.LoteId, request.PlantillaDocumentoId, request.MovimientoId)
        {
            PerformedByUserId = GetCurrentUserId(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code.EndsWith("NotFound", StringComparison.Ordinal)
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>Detalle del documento generado: campos con su valor y origen (inferido/manual/dictado).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentoExportacionDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<ObtenerDocumentoQuery, Result<DocumentoExportacionDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ObtenerDocumentoQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return Ok(result.Value);
    }

    /// <summary>Confirma el documento: aplica ediciones, valida obligatorios y lo finaliza.</summary>
    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(
        Guid id,
        [FromBody] ConfirmarDocumentoRequest request,
        [FromServices] ICommandHandler<ConfirmarDocumentoCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmarDocumentoCommand(id, request.Campos ?? [])
        {
            PerformedByUserId = GetCurrentUserId(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        };

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var status = result.Error.Code switch
        {
            "Documento.NotFound" => StatusCodes.Status404NotFound,
            "Documento.YaConfirmado" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
    }

    private Guid? GetCurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

/// <summary>Body para generar un borrador.</summary>
public sealed record GenerarBorradorRequest(Guid LoteId, Guid PlantillaDocumentoId, Guid? MovimientoId);

/// <summary>Body para confirmar: las ediciones de campos del usuario.</summary>
public sealed record ConfirmarDocumentoRequest(IReadOnlyList<CampoEdicion> Campos);
