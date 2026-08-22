namespace Papasur.Application.Abstractions;

/// <summary>
/// Parámetros de paginación comunes a TODOS los endpoints de listado.
/// Page es 1-based; PageSize se acota a [1, MaxPageSize] para que un cliente no pueda
/// pedir la tabla entera.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public PageRequest()
    {
    }

    public PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    private readonly int _page = 1;

    private readonly int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// Página de resultados. La envoltura { items, page, pageSize, total } es la que espera el
/// front en TODOS los listados (contrato de API §0); el resto son campos derivados.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(PageRequest page) => new([], page.Page, page.PageSize, 0);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, PageSize, Total);
}
