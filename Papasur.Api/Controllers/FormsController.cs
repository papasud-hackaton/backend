using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Commands;
using Papasur.Application.ExportForms.Commands.CreateForm;
using Papasur.Application.ExportForms.Commands.GenerateFormDocuments;
using Papasur.Application.ExportForms.Commands.TransitionForm;
using Papasur.Application.ExportForms.Commands.UpdateForm;
using Papasur.Application.ExportForms.Ports;
using Papasur.Application.ExportForms.Queries.GetFormById;
using Papasur.Application.ExportForms.Queries.GetForms;

namespace Papasur.Api.Controllers;

/// <summary>
/// Formularios de exportación (contrato §5): el núcleo. Un formulario = un envío.
///
/// Dos reglas que se aplican acá y no en el front: un agente sólo ve y toca lo propio, y el
/// servidor no confía en el cliente (§0.2) — totales, advertencias y estado los pone el backend.
/// </summary>
[Route("api/v1/forms")]
public class FormsController : ApiControllerBase
{
    private const string MeFilter = "me";

    /// <summary>
    /// Listado. status es REPETIBLE (?status=draft&amp;status=submitted); createdBy=me filtra por
    /// el usuario del token. Un agente recibe sólo los propios, sin importar qué mande.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExportFormSummaryDto>>> List(
        [FromServices] IQueryHandler<GetFormsQuery, Result<PagedResult<ExportFormSummaryDto>>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery(Name = "status")] string[]? statuses = null,
        [FromQuery] string? createdBy = null,
        [FromQuery] string? search = null)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var createdByFilter = createdBy switch
        {
            null or "" => (Guid?)null,
            MeFilter => actor.Id,
            var raw when Guid.TryParse(raw, out var id) => id,
            _ => null,
        };

        var result = await handler.Handle(
            new GetFormsQuery(
                new PageRequest(page, pageSize),
                new FormFilter(statuses is { Length: > 0 } ? statuses : null, createdByFilter, search),
                actor),
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExportFormDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetFormByIdQuery, Result<ExportFormDto>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new GetFormByIdQuery(id, actor), cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error == GetFormByIdQueryHandler.Forbidden
            ? Fail(StatusCodes.Status403Forbidden, result.Error)
            : Fail(StatusCodes.Status404NotFound, result.Error);
    }

    /// <summary>Crea el borrador. El servidor asigna id, code, status, version y autor.</summary>
    [HttpPost]
    public async Task<ActionResult<ExportFormDto>> Create(
        [FromBody] FormRequest request,
        [FromServices] ICommandHandler<CreateFormCommand, Result<ExportFormDto>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new CreateFormCommand(request.ToFields(), actor), cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status400BadRequest, result.Error)
            : Created($"/api/v1/forms/{result.Value.Id}", result.Value);
    }

    /// <summary>
    /// Edición con bloqueo optimista (§0.1). El header If-Match lleva la versión que el cliente
    /// tenía; si no coincide, 409 con el estado actual y NO se escribe nada.
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ExportFormDto>> Update(
        Guid id,
        [FromBody] FormRequest request,
        [FromServices] ICommandHandler<UpdateFormCommand, Result<UpdateFormResult>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        int? ifMatch = Request.Headers.TryGetValue("If-Match", out var raw)
            && int.TryParse(raw.ToString().Trim('"'), out var version)
                ? version
                : null;

        var result = await handler.Handle(
            new UpdateFormCommand(id, request.ToFields(), ifMatch, actor),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == UpdateFormCommandHandler.NotEditable)
            {
                return Fail(StatusCodes.Status403Forbidden, result.Error);
            }

            return result.Error.Code == "Form.NotFound"
                ? Fail(StatusCodes.Status404NotFound, result.Error)
                : Fail(StatusCodes.Status400BadRequest, result.Error);
        }

        if (result.Value.IsConflict)
        {
            return Conflict(new
            {
                message = "Alguien más editó este formulario mientras lo tenías abierto.",
                code = "version_conflict",
                currentVersion = result.Value.ConflictVersion,
                current = result.Value.Form,
            });
        }

        return Ok(result.Value.Form);
    }

    /// <summary>
    /// La máquina de estados. Un 403 acá trae el motivo REAL en message: el front lo muestra
    /// tal cual ("Resolvé las advertencias bloqueantes.").
    /// </summary>
    [HttpPost("{id:guid}/transition")]
    public async Task<ActionResult<ExportFormDto>> Transition(
        Guid id,
        [FromBody] TransitionRequest request,
        [FromServices] ICommandHandler<TransitionFormCommand, Result<ExportFormDto>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(
            new TransitionFormCommand(id, request.Action ?? string.Empty, request.ReviewNotes, request.Reason, actor),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Code switch
        {
            TransitionFormCommandHandler.NotAllowedCode => Fail(StatusCodes.Status403Forbidden, result.Error),
            "Form.NotFound" => Fail(StatusCodes.Status404NotFound, result.Error),
            _ => Fail(StatusCodes.Status400BadRequest, result.Error),
        };
    }

    /// <summary>Genera los documentos del envío cruzando requisitos con la trazabilidad congelada.</summary>
    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<GeneratedDocumentDto>>> GenerateDocuments(
        Guid id,
        [FromServices] ICommandHandler<GenerateFormDocumentsCommand, Result<IReadOnlyList<GeneratedDocumentDto>>> handler,
        CancellationToken cancellationToken)
    {
        if (CurrentActor is not { } actor)
        {
            return Unauthorized();
        }

        var result = await handler.Handle(new GenerateFormDocumentsCommand(id, actor), cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error.Code == "Form.NotFound"
            ? Fail(StatusCodes.Status404NotFound, result.Error)
            : Fail(StatusCodes.Status400BadRequest, result.Error);
    }
}

/// <summary>
/// Cuerpo aceptado en el alta y la edición. Es la LISTA BLANCA del contrato §0.2: lo que no
/// está acá se ignora aunque venga en el body.
/// </summary>
public sealed record FormRequest(
    Guid? CustomerId,
    string? DestinationCountryCode,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? Incoterm,
    string? Currency,
    string? PaymentTerms,
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyList<FormItemRequest>? Items,
    IReadOnlyDictionary<string, string>? RequirementValues)
{
    public FormFieldsInput ToFields() => new(
        CustomerId,
        DestinationCountryCode,
        PortOfLoading,
        PortOfDischarge,
        Incoterm,
        Currency,
        PaymentTerms,
        ValidUntil,
        Notes,
        Items?.Select(i => new FormItemInput(i.LotId, i.QuantityKg, i.PackagingType, i.UnitPrice)).ToList(),
        RequirementValues);
}

public sealed record FormItemRequest(Guid LotId, decimal QuantityKg, string PackagingType, decimal UnitPrice);

public sealed record TransitionRequest(string? Action, string? ReviewNotes, string? Reason);
