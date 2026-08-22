using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandHandler(IUserRepository users, IAuditRepository audit)
    : ICommandHandler<SetUserStatusCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(SetUserStatusCommand command, CancellationToken cancellationToken)
    {
        if (!UserStatuses.Exists(command.Status))
        {
            return Result.Failure<UserDto>(new Error("User.StatusInvalid", "El estado indicado no existe."));
        }

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(new Error("User.NotFound", "El usuario no existe."));
        }

        // Un admin no puede dejarse afuera del sistema a sí mismo.
        if (command.Status == UserStatuses.Inactive && command.Actor?.Id == user.Id)
        {
            return Result.Failure<UserDto>(new Error(
                "User.CannotDeactivateSelf",
                "No podés desactivar tu propio usuario."));
        }

        if (user.Status == command.Status)
        {
            return Result.Success(user.ToDto());
        }

        var anterior = user.Status;
        user.Status = command.Status;

        await users.UpdateAsync(user, cancellationToken);

        if (command.Actor is { } actor)
        {
            await audit.AddAsync(
                AuditFactory.Create(
                    actor,
                    command.Status == UserStatuses.Inactive
                        ? AuditActions.UserDeactivated
                        : AuditActions.UserUpdated,
                    AuditEntityTypes.User,
                    user.Id.ToString(),
                    $"{(command.Status == UserStatuses.Inactive ? "Baja" : "Alta")} de {user.Email}.",
                    AuditFactory.ChangeSet(("status", anterior, command.Status))),
                cancellationToken);
        }

        return Result.Success(user.ToDto());
    }
}
