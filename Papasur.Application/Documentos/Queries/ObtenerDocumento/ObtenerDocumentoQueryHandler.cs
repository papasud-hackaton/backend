using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Documentos.Ports;

namespace Papasur.Application.Documentos.Queries.ObtenerDocumento;

public sealed class ObtenerDocumentoQueryHandler(IDocumentoRepository documentos)
    : IQueryHandler<ObtenerDocumentoQuery, Result<DocumentoExportacionDto>>
{
    public async Task<Result<DocumentoExportacionDto>> Handle(
        ObtenerDocumentoQuery query,
        CancellationToken cancellationToken)
    {
        var doc = await documentos.GetByIdAsync(query.Id, cancellationToken);

        if (doc is null)
        {
            return Result.Failure<DocumentoExportacionDto>(
                new Error("Documento.NotFound", "El documento indicado no existe."));
        }

        var campos = doc.Valores
            .OrderBy(v => v.CampoPlantilla.Orden)
            .Select(v => new ValorCampoDto(
                v.CampoPlantillaId,
                v.CampoPlantilla.Clave,
                v.CampoPlantilla.Etiqueta,
                v.CampoPlantilla.TipoDato,
                v.CampoPlantilla.Obligatorio,
                v.CampoPlantilla.Orden,
                v.Valor,
                v.Origen.ToString(),
                v.Confirmado,
                v.InferidoDesde))
            .ToList();

        return Result.Success(new DocumentoExportacionDto(
            doc.Id,
            doc.LoteId,
            doc.Lote?.Codigo ?? string.Empty,
            doc.Lote?.Variedad?.Nombre ?? string.Empty,
            doc.MovimientoId,
            doc.Movimiento?.Dtv,
            doc.PlantillaDocumentoId,
            doc.PlantillaDocumento?.Nombre ?? string.Empty,
            doc.PlantillaDocumento?.Tipo ?? string.Empty,
            doc.VersionPlantilla,
            doc.StatusId,
            doc.Status?.Name ?? string.Empty,
            doc.CreatedAt,
            doc.ConfirmedAt,
            campos));
    }
}
