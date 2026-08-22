using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.ExportForms.Ports;

namespace Papasur.Application.ExportForms.Queries.GetForms;

public sealed record GetFormsQuery(PageRequest Page, FormFilter Filter, Actor Actor)
    : IQuery<Result<PagedResult<ExportFormSummaryDto>>>;
