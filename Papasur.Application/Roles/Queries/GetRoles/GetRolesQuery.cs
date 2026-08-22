using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Roles.Queries.GetRoles;

public sealed record GetRolesQuery(PageRequest Page) : IQuery<PagedResult<RoleDto>>;
