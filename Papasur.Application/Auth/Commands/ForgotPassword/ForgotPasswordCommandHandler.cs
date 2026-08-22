using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Genera el token de recuperación y dispara el correo. Contrato §1: devuelve éxito SIEMPRE,
/// exista o no la cuenta — por la misma razón que el login no distingue casos.
/// El token viaja en claro sólo en el enlace; en la base se guarda su hash.
/// </summary>
public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IInvitationSender sender,
    IAuditRepository audit)
    : ICommandHandler<ForgotPasswordCommand, Result>
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(2);

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Success();
        }

        var user = await users.GetByEmailAsync(email, cancellationToken);

        // Cuenta inexistente o inactiva: se responde igual, sin hacer nada.
        if (user is null || user.Status == UserStatuses.Inactive)
        {
            return Result.Success();
        }

        var now = DateTime.UtcNow;

        // Un token vigente por vez: pedir uno nuevo invalida el anterior.
        await tokens.InvalidateAllForUserAsync(user.Id, now, cancellationToken);

        var plainToken = ResetTokens.NewToken();

        await tokens.AddAsync(
            new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ResetTokens.Hash(plainToken),
                ExpiresAt = now.Add(Vigencia),
                CreatedAt = now,
            },
            cancellationToken);

        await sender.SendPasswordResetAsync(user.Email, user.FirstName, plainToken, cancellationToken);

        await audit.AddAsync(
            AuditFactory.Create(
                new Actor(user.Id, user.FullName, user.Role?.Name ?? string.Empty, command.IpAddress),
                AuditActions.UserPasswordResetRequested,
                AuditEntityTypes.User,
                user.Id.ToString()),
            cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Tokens opacos de un solo uso. El hash es SHA-256 SIN salt a propósito: tiene que ser
/// determinístico para poder buscar el token en la base, y el token ya trae 256 bits de
/// entropía (a diferencia de una contraseña, que sí necesita salt + PBKDF2).
/// </summary>
public static class ResetTokens
{
    public static string NewToken()
        => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string Hash(string token)
        => Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();
}
