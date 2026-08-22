using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;

namespace Papasur.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IPasswordHasher passwordHasher,
    IAuditRepository audit)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (command.Actor is not { } actor)
        {
            return Result.Failure(new Error("Auth.Unauthenticated", "Necesitás iniciar sesión."));
        }

        var user = await users.GetByIdAsync(actor.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(new Error("User.NotFound", "El usuario no existe."));
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(new Error(
                "Auth.CurrentPasswordInvalid",
                "La contraseña actual no es correcta."));
        }

        if (PasswordPolicy.Validate(command.NewPassword) is { } policyError)
        {
            return Result.Failure(policyError);
        }

        if (passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            return Result.Failure(new Error(
                "Auth.PasswordUnchanged",
                "La contraseña nueva debe ser distinta de la actual."));
        }

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        await users.UpdateAsync(user, cancellationToken);

        // Cambiar la clave invalida cualquier enlace de recuperación pendiente.
        await tokens.InvalidateAllForUserAsync(user.Id, DateTime.UtcNow, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                actor,
                AuditActions.UserPasswordChanged,
                AuditEntityTypes.User,
                user.Id.ToString()),
            cancellationToken);

        return Result.Success();
    }
}
