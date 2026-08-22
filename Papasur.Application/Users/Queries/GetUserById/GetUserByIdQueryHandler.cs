using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserRepository users)
    : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(new Error("User.NotFound", "El usuario no existe."))
            : Result.Success(user.ToDto());
    }
}
