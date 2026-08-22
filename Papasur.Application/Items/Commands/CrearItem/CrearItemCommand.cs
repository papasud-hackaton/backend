using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Items.Commands.CrearItem;

public sealed record CrearItemCommand(string Nombre, decimal Valor, DateTime FechaRegistro)
    : ICommand<Result<Guid>>;
