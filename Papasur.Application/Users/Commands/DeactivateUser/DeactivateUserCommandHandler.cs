using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(IUserRepository users, IAuditRepository audit)
    : ICommandHandler<DeactivateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(new Error("User.NotFound", "El usuario no existe."));
        }

        // Un admin no puede dejarse afuera del sistema a sí mismo.
        if (command.Actor?.Id == user.Id)
        {
            return Result.Failure<UserDto>(new Error(
                "User.CannotDeactivateSelf",
                "No podés desactivar tu propio usuario."));
        }

        if (user.Status == UserStatuses.Inactive)
        {
            return Result.Success(user.ToDto());
        }

        var anterior = user.Status;
        user.Status = UserStatuses.Inactive;

        await users.UpdateAsync(user, cancellationToken);

        if (command.Actor is { } actor)
        {
            await audit.AddAsync(
                AuditFactory.Create(
                    actor,
                    AuditActions.UserDeactivated,
                    AuditEntityTypes.User,
                    user.Id.ToString(),
                    $"Baja de {user.Email}.",
                    AuditFactory.ChangeSet(("status", anterior, UserStatuses.Inactive))),
                cancellationToken);
        }

        return Result.Success(user.ToDto());
    }
}
