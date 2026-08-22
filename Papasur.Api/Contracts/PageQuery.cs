using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;

namespace Papasur.Api.Contracts;

/// <summary>
/// Parámetros de paginación de la query string (?page=1&amp;pageSize=20), comunes a
/// TODOS los endpoints de listado. PageRequest se encarga de acotar los valores.
/// </summary>
public sealed class PageQuery
{
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public PageRequest ToPageRequest() => new(Page, PageSize);
}
