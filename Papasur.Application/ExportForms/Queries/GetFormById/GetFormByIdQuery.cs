using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.ExportForms.Queries.GetFormById;

public sealed record GetFormByIdQuery(Guid Id, Actor Actor) : IQuery<Result<ExportFormDto>>;
