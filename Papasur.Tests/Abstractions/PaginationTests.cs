using Papasur.Application.Abstractions;

namespace Papasur.Tests.Abstractions;

public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_SeAcotaAlMinimo(int requested, int expected)
        => Assert.Equal(expected, new PageRequest(requested, 20).Page);

    [Theory]
    [InlineData(0, PageRequest.DefaultPageSize)]
    [InlineData(500, PageRequest.MaxPageSize)]
    [InlineData(50, 50)]
    public void PageSize_SeAcotaAlRangoValido(int requested, int expected)
        => Assert.Equal(expected, new PageRequest(1, requested).PageSize);

    [Fact]
    public void Skip_SaltaLasPaginasAnteriores()
        => Assert.Equal(40, new PageRequest(3, 20).Skip);

    [Fact]
    public void PagedResult_CalculaTotalPagesYNavegacion()
    {
        var result = new PagedResult<int>([1, 2, 3], Page: 2, PageSize: 3, Total: 10);

        Assert.Equal(4, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void Map_ConservaLosMetadatosDePaginacion()
    {
        var result = new PagedResult<int>([1, 2], Page: 2, PageSize: 2, Total: 7);

        var mapped = result.Map(i => i.ToString());

        Assert.Equal(["1", "2"], mapped.Items);
        Assert.Equal(2, mapped.Page);
        Assert.Equal(7, mapped.Total);
        Assert.Equal(4, mapped.TotalPages);
    }
}
