using Papasur.Application.Abstractions;
using Papasur.Application.Items.Commands.CrearItem;
using Papasur.Application.Items.Ports;
using Papasur.Domain.Items;

namespace Papasur.Tests.Items;

public class CrearItemCommandHandlerTests
{
    private sealed class FakeItemRepository : IItemRepository
    {
        public List<Item> Guardados { get; } = [];

        public Task AddAsync(Item item, CancellationToken cancellationToken)
        {
            Guardados.Add(item);
            return Task.CompletedTask;
        }

        public Task<PagedResult<Item>> ListAsync(PageRequest page, CancellationToken cancellationToken)
            => Task.FromResult(new PagedResult<Item>(
                Guardados.Skip(page.Skip).Take(page.PageSize).ToList(),
                page.Page,
                page.PageSize,
                Guardados.Count));
    }

    [Fact]
    public async Task Handle_ConDatosValidos_PersisteYDevuelveId()
    {
        var repo = new FakeItemRepository();
        var handler = new CrearItemCommandHandler(repo);
        var command = new CrearItemCommand("Item de ejemplo", 1234.5m, DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        var guardado = Assert.Single(repo.Guardados);
        Assert.Equal("Item de ejemplo", guardado.Nombre);
        Assert.Equal(1234.5m, guardado.Valor);
    }

    [Fact]
    public async Task Handle_SinNombre_DevuelveFailureSinPersistir()
    {
        var repo = new FakeItemRepository();
        var handler = new CrearItemCommandHandler(repo);
        var command = new CrearItemCommand("  ", 1m, DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Item.NombreRequerido", result.Error.Code);
        Assert.Empty(repo.Guardados);
    }
}
