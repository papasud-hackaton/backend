namespace Papasur.Application.DocumentTypes.Queries.GetDocumentTypes;

/// <summary>
/// Campo que exige un documento (contrato §4). El <c>Path</c> dice de dónde se autocompleta:
/// es lo que hace que el motor de requisitos sea data-driven de verdad.
/// </summary>
public sealed record RequirementFieldDto(
    string Key,
    string Label,
    string Source,
    bool Required,
    string? Path,
    string? Hint);

/// <summary>
/// Definición de un documento y sus requisitos. Cambiar plantillas es cambiar ESTA respuesta,
/// no código: sale de plantilla_documento + campo_plantilla.
/// </summary>
public sealed record DocumentTypeDto(
    string Code,
    string Name,
    string? IssuingBody,
    string? AppliesWhen,
    IReadOnlyList<RequirementFieldDto> Fields);
