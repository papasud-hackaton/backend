using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;

namespace Papasur.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IUserRepository users)
    : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var page = await users.ListAsync(
            query.Page,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            string.IsNullOrWhiteSpace(query.Role) ? null : query.Role.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToLowerInvariant(),
            cancellationToken);

        return page.Map(u => u.ToDto());
    }
}
