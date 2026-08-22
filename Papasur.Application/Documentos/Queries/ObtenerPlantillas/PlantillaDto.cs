namespace Papasur.Application.Documentos.Queries.ObtenerPlantillas;

/// <summary>Proyección de una plantilla documental para elegir cuál usar al generar un documento.</summary>
public sealed record PlantillaDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Organismo,
    string? PaisDestino,
    int Version,
    bool Activa,
    int CantidadCampos);
