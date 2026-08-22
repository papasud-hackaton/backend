using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.ResetUserPassword;

public sealed class ResetUserPasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuditRepository audit)
    : ICommandHandler<ResetUserPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (PasswordPolicy.Validate(command.NewPassword) is { } error)
        {
            return Result.Failure(error);
        }

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new Error("User.NotFound", "El usuario no existe."));
        }

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        await users.UpdateAsync(user, cancellationToken);

        await audit.AddAsync(
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = command.PerformedByUserId ?? user.Id,
                Action = AuditActions.PasswordReset,
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                Detail = $"Reseteo de contraseña de {user.Email}.",
                IpAddress = command.IpAddress,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Result.Success();
    }
}
