using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Roles.Ports;

namespace Papasur.Application.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler(IRoleRepository roles)
    : IQueryHandler<GetRolesQuery, PagedResult<RoleDto>>
{
    public async Task<PagedResult<RoleDto>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var page = await roles.ListAsync(query.Page, cancellationToken);

        return page.Map(r => new RoleDto(r.Id, r.Name, r.Description));
    }
}
