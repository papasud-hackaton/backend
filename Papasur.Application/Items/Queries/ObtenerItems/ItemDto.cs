namespace Papasur.Application.Items.Queries.ObtenerItems;

public sealed record ItemDto(Guid Id, string Nombre, decimal Valor, DateTime FechaRegistro);
