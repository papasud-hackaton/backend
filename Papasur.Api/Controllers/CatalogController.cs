using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Customers.Commands.CreateCustomer;
using Papasur.Application.Customers.Queries.GetCustomers;
using Papasur.Application.Locations.Queries.GetLocations;
using Papasur.Application.Lots.Ports;
using Papasur.Application.Lots.Queries.GetLotById;
using Papasur.Application.Lots.Queries.GetLots;

namespace Papasur.Api.Controllers;

/// <summary>
/// Ubicaciones de stock (contrato §3). Son cuatro y sólo se leen.
/// Devuelve un array, no una página: así lo pide el contrato para los catálogos chicos.
/// </summary>
[Route("api/v1/locations")]
public class LocationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StorageLocationDto>>> List(
        [FromServices] IQueryHandler<GetLocationsQuery, Result<IReadOnlyList<StorageLocationDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetLocationsQuery(), cancellationToken);

        return Ok(result.Value);
    }
}

/// <summary>Clientes / importadores (contrato §3).</summary>
[Route("api/v1/customers")]
public class CustomersController : ApiControllerBase
{
    /// <summary>Búsqueda por nombre. Array completo, sin paginar.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromServices] IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null)
    {
        var result = await handler.Handle(new GetCustomersQuery(search), cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>Alta rápida desde el paso 1 del wizard, sin salir del formulario.</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        [FromServices] ICommandHandler<CreateCustomerCommand, Result<CustomerDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateCustomerCommand(
                request.Name,
                request.TaxId,
                request.CountryCode,
                request.Address,
                request.City,
                request.ContactName,
                request.ContactEmail,
                CurrentActor),
            cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : Created($"/api/v1/customers/{result.Value.Id}", result.Value);
    }
}

public sealed record CreateCustomerRequest(
    string Name,
    string TaxId,
    string CountryCode,
    string Address,
    string City,
    string? ContactName,
    string? ContactEmail);

/// <summary>
/// Lotes en el shape del contrato §3: el saldo sale de los movimientos reales de la planilla,
/// no de un campo guardado.
/// </summary>
[Route("api/v1/lots")]
public class LotsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SeedLotDto>>> List(
        [FromServices] IQueryHandler<GetLotsQuery, Result<PagedResult<SeedLotDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null)
    {
        var result = await handler.Handle(
            new GetLotsQuery(
                new PageRequest(page, pageSize, PageRequest.CatalogPageSize),
                new LotFilter(search, locationId, category, status)),
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SeedLotDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetLotByIdQuery, Result<SeedLotDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetLotByIdQuery(id), cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status404NotFound, result.Error)
            : Ok(result.Value);
    }
}
