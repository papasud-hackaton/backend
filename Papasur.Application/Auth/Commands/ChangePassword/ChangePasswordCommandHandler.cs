using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuditRepository audit)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new Error("User.NotFound", "El usuario no existe."));
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(new Error("Auth.CurrentPasswordInvalid", "La contraseña actual es incorrecta."));
        }

        if (PasswordPolicy.Validate(command.NewPassword) is { } error)
        {
            return Result.Failure(error);
        }

        if (passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            return Result.Failure(new Error(
                "Auth.PasswordUnchanged",
                "La contraseña nueva debe ser distinta de la actual."));
        }

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        await users.UpdateAsync(user, cancellationToken);

        await audit.AddAsync(
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Action = AuditActions.PasswordChanged,
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                IpAddress = command.IpAddress,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Result.Success();
    }
}
