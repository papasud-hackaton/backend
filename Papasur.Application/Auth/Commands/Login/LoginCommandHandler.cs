using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;

namespace Papasur.Application.Auth.Commands.Login;

/// <summary>
/// Autentica y emite el JWT. Todo fallo devuelve el MISMO error genérico
/// (no se revela si el correo existe o si la contraseña es incorrecta).
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IAuditRepository audit)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    private static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Correo o contraseña incorrectos.");

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        var user = await users.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                await audit.AddAsync(
                    new AuditEntry
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Action = AuditActions.LoginFailed,
                        EntityType = nameof(Domain.Users.User),
                        EntityId = user.Id.ToString(),
                        Detail = "Contraseña incorrecta.",
                        IpAddress = command.IpAddress,
                        OccurredAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }

            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(new Error("Auth.UserInactive", "El usuario está inactivo."));
        }

        var token = tokenGenerator.Generate(user);

        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        await audit.AddAsync(
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Action = AuditActions.Login,
                EntityType = nameof(Domain.Users.User),
                EntityId = user.Id.ToString(),
                IpAddress = command.IpAddress,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Result.Success(new LoginResponse(
            token.Token,
            token.ExpiresAt,
            user.Id,
            user.Name,
            user.Email,
            user.Role.Name));
    }
}
