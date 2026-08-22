using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IUserRepository users)
    : IQueryHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(new Error("User.NotFound", "El usuario no existe."));
        }

        return Result.Success(new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.EmployeeNumber,
            user.RoleId,
            user.Role?.Name ?? string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt));
    }
}
