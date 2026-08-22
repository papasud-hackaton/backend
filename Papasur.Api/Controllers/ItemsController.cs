using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Queries.ObtenerItems;

namespace Papasur.Api.Controllers;

/// <summary>
/// Controller de ejemplo: inyecta handlers CQRS directamente (un handler por operación)
/// y mapea Result de negocio → ProblemDetails.
/// </summary>
[ApiController]
[Route("api/v1/items")]
public class ItemsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemDto>>> Listar(
        [FromServices] IQueryHandler<ObtenerItemsQuery, PagedResult<ItemDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var items = await handler.Handle(
            new ObtenerItemsQuery(new PageRequest(page, pageSize)),
            cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Crear(
        [FromBody] CrearItemCommand command,
        [FromServices] ICommandHandler<CrearItemCommand, Result<Guid>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: result.Error.Code,
                detail: result.Error.Message);
        }

        return CreatedAtAction(nameof(Listar), new { id = result.Value }, result.Value);
    }
}
