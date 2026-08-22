using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Ports;
using Papasur.Domain.Users;

namespace Papasur.Application.ExportForms.Queries.GetFormById;

public sealed class GetFormByIdQueryHandler(IExportFormRepository forms, FormAssembler assembler)
    : IQueryHandler<GetFormByIdQuery, Result<ExportFormDto>>
{
    /// <summary>Error que el controller mapea a 403: un agente pidiendo un formulario ajeno.</summary>
    public static readonly Error Forbidden =
        new("Form.Forbidden", "No tenés acceso a este formulario.");

    public async Task<Result<ExportFormDto>> Handle(GetFormByIdQuery query, CancellationToken cancellationToken)
    {
        var form = await forms.GetByIdAsync(query.Id, cancellationToken);

        if (form is null)
        {
            return Result.Failure<ExportFormDto>(new Error("Form.NotFound", "Formulario no encontrado."));
        }

        if (query.Actor.Role == RoleNames.Agent && form.CreatedByUserId != query.Actor.Id)
        {
            return Result.Failure<ExportFormDto>(Forbidden);
        }

        return Result.Success(await assembler.ToDtoAsync(form, cancellationToken));
    }
}
