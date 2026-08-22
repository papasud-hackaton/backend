using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Commands.ForgotPassword;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Consume el token de recuperación y define la contraseña. Un token vencido o ya usado
/// devuelve el error de vencido (410 en el controller), que es lo que el front espera para
/// ofrecer pedir uno nuevo. Es también el camino por el que un usuario invitado se activa.
/// </summary>
public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IPasswordHasher passwordHasher,
    IAuditRepository audit)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public static readonly Error Expired =
        new("Auth.TokenExpired", "El enlace venció. Pedí uno nuevo.");

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure(Expired);
        }

        if (PasswordPolicy.Validate(command.Password) is { } policyError)
        {
            return Result.Failure(policyError);
        }

        var now = DateTime.UtcNow;
        var token = await tokens.GetByHashAsync(ResetTokens.Hash(command.Token), cancellationToken);

        if (token is null || !token.IsUsable(now))
        {
            return Result.Failure(Expired);
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null || user.Status == UserStatuses.Inactive)
        {
            return Result.Failure(Expired);
        }

        user.PasswordHash = passwordHasher.Hash(command.Password);

        // Definir la contraseña es lo que activa a un usuario invitado.
        if (user.Status == UserStatuses.Invited)
        {
            user.Status = UserStatuses.Active;
        }

        await users.UpdateAsync(user, cancellationToken);

        token.UsedAt = now;
        await tokens.UpdateAsync(token, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                new Actor(user.Id, user.FullName, user.Role?.Name ?? string.Empty, command.IpAddress),
                AuditActions.UserPasswordChanged,
                AuditEntityTypes.User,
                user.Id.ToString()),
            cancellationToken);

        return Result.Success();
    }
}
