using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Documentos.Commands.ConfirmarDocumento;

/// <summary>
/// Confirmación humana explícita de un documento: aplica las ediciones del usuario sobre los campos,
/// valida que estén los obligatorios y marca el documento como finalizado. Es el paso que la consigna
/// exige antes de dar por bueno cualquier dato inferido (la IA sugiere, la persona confirma).
/// </summary>
public sealed record ConfirmarDocumentoCommand(
    Guid DocumentoId,
    IReadOnlyList<CampoEdicion> Campos) : ICommand<Result>
{
    public Guid? PerformedByUserId { get; init; }

    public string? IpAddress { get; init; }
}

/// <summary>Edición de un campo antes de confirmar. PorDictado marca el valor como capturado por voz.</summary>
public sealed record CampoEdicion(Guid CampoPlantillaId, string? Valor, bool PorDictado = false);
