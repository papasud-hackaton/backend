using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Ports;
using Papasur.Domain.Users;

namespace Papasur.Application.ExportForms.Queries.GetForms;

public sealed class GetFormsQueryHandler(IExportFormRepository forms, FormAssembler assembler)
    : IQueryHandler<GetFormsQuery, Result<PagedResult<ExportFormSummaryDto>>>
{
    public async Task<Result<PagedResult<ExportFormSummaryDto>>> Handle(
        GetFormsQuery query,
        CancellationToken cancellationToken)
    {
        // Un agente recibe SÓLO los propios, filtrado acá, sin importar qué mande en createdBy
        // (contrato §5). El chequeo del front es cosmético; la autorización real es ésta.
        var filter = query.Actor.Role == RoleNames.Agent
            ? query.Filter with { CreatedBy = query.Actor.Id }
            : query.Filter;

        var page = await forms.ListAsync(query.Page, filter, cancellationToken);
        var summaries = await assembler.ToSummariesAsync(page.Items, cancellationToken);

        return Result.Success(new PagedResult<ExportFormSummaryDto>(
            summaries,
            page.Page,
            page.PageSize,
            page.Total));
    }
}
