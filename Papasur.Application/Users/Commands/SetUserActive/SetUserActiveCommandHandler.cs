using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.SetUserActive;

public sealed class SetUserActiveCommandHandler(IUserRepository users, IAuditRepository audit)
    : ICommandHandler<SetUserActiveCommand, Result>
{
    public async Task<Result> Handle(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new Error("User.NotFound", "El usuario no existe."));
        }

        // Un admin no puede desactivarse a sí mismo: se quedaría afuera del sistema.
        if (!command.IsActive && command.PerformedByUserId == user.Id)
        {
            return Result.Failure(new Error(
                "User.CannotDeactivateSelf",
                "No podés desactivar tu propio usuario."));
        }

        if (user.IsActive == command.IsActive)
        {
            return Result.Success();
        }

        user.IsActive = command.IsActive;
        await users.UpdateAsync(user, cancellationToken);

        await audit.AddAsync(
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = command.PerformedByUserId ?? user.Id,
                Action = command.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                Detail = $"{(command.IsActive ? "Alta" : "Baja")} de {user.Email}.",
                IpAddress = command.IpAddress,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Result.Success();
    }
}
