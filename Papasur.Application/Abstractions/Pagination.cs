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

    /// <summary>
    /// Tope ampliado para el índice de lotes. El front pide pageSize=500 UNA vez y lo cachea
    /// (contrato §3), porque de ahí salen los saldos que alimentan las advertencias en vivo.
    /// Son ~150 lotes: entra en una página sin castigar a nadie.
    /// </summary>
    public const int CatalogPageSize = 500;

    public PageRequest()
    {
    }

    public PageRequest(int page, int pageSize) : this(page, pageSize, MaxPageSize)
    {
    }

    public PageRequest(int page, int pageSize, int maxPageSize)
    {
        _maxPageSize = maxPageSize < 1 ? MaxPageSize : maxPageSize;
        Page = page;
        PageSize = pageSize;
    }

    private readonly int _maxPageSize = MaxPageSize;

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
        init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, _maxPageSize);
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
