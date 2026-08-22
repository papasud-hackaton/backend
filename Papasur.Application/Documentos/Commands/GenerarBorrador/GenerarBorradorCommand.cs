using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Documentos.Commands.GenerarBorrador;

/// <summary>
/// Genera un borrador de documento de exportación para un lote a partir de una plantilla,
/// pre-completando por inferencia. Opcionalmente se ancla a un movimiento/despacho concreto
/// (aporta remito, kilos, DTV, etc.).
/// </summary>
public sealed record GenerarBorradorCommand(
    Guid LoteId,
    Guid PlantillaDocumentoId,
    Guid? MovimientoId) : ICommand<Result<Guid>>
{
    public Guid? PerformedByUserId { get; init; }

    public string? IpAddress { get; init; }
}
