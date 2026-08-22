using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Settings.Queries.GetOrganization;

public sealed record GetOrganizationQuery : IQuery<IReadOnlyDictionary<string, string>>;
