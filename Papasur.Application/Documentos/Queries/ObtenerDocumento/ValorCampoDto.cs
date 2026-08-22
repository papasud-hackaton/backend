namespace Papasur.Application.Documentos.Queries.ObtenerDocumento;

/// <summary>
/// Un campo del documento generado, con su valor y —clave para la vertical— el origen del dato
/// (Inferido / Manual / Dictado) y si ya fue confirmado por una persona.
/// </summary>
public sealed record ValorCampoDto(
    Guid CampoPlantillaId,
    string Clave,
    string Etiqueta,
    string TipoDato,
    bool Obligatorio,
    int Orden,
    string? Valor,
    string Origen,
    bool Confirmado,
    string? InferidoDesde);
