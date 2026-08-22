using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Items.Ports;
using Papasur.Domain.Items;

namespace Papasur.Application.Items.Commands.CrearItem;

public sealed class CrearItemCommandHandler(IItemRepository repository)
    : ICommandHandler<CrearItemCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CrearItemCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            return Result.Failure<Guid>(new Error("Item.NombreRequerido", "El nombre del item es obligatorio."));
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre.Trim(),
            Valor = command.Valor,
            FechaRegistro = command.FechaRegistro,
            CreatedAt = DateTime.UtcNow,
        };

        await repository.AddAsync(item, cancellationToken);

        return Result.Success(item.Id);
    }
}
