using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
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
            query.RoleId,
            query.IsActive,
            cancellationToken);

        return page.Map(u => new UserDto(
            u.Id,
            u.Name,
            u.Email,
            u.EmployeeNumber,
            u.RoleId,
            u.Role?.Name ?? string.Empty,
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt));
    }
}
