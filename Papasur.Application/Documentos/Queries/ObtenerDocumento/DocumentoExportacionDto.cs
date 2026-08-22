namespace Papasur.Application.Documentos.Queries.ObtenerDocumento;

/// <summary>
/// Documento de exportación generado, tal como lo consume la pantalla de revisión: cabecera
/// (lote, plantilla, estado) + la lista de campos con su valor y origen.
/// </summary>
public sealed record DocumentoExportacionDto(
    Guid Id,
    Guid LoteId,
    string LoteCodigo,
    string Variedad,
    Guid? MovimientoId,
    string? Dtv,
    Guid PlantillaDocumentoId,
    string Plantilla,
    string Tipo,
    int VersionPlantilla,
    int StatusId,
    string Status,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    IReadOnlyList<ValorCampoDto> Campos);
