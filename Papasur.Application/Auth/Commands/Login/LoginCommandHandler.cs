using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Commands.Login;

/// <summary>
/// Autentica y emite el JWT. Contrato §1: el 401 tiene que ser IDÉNTICO para usuario
/// inexistente y contraseña incorrecta — si difieren, se filtra qué cuentas existen.
/// La cuenta desactivada es el único caso que se distingue (403), porque el usuario
/// necesita saber que tiene que hablar con un admin.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IAuditRepository audit)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "El correo o la contraseña no son correctos.");

    public static readonly Error Disabled =
        new("Auth.Disabled", "Tu cuenta está desactivada. Contactá a un administrador.");

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        var user = await users.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Sin usuario, sin contraseña definida (invitado) o con contraseña incorrecta: el MISMO error.
        if (user is null
            || string.IsNullOrEmpty(user.PasswordHash)
            || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(Disabled);
        }

        var token = tokenGenerator.Generate(user);

        user.LastLoginAt = DateTime.UtcNow;
        await users.UpdateAsync(user, cancellationToken);

        var actor = new Actor(user.Id, user.FullName, user.Role.Name, command.IpAddress);

        await audit.AddAsync(
            AuditFactory.Create(actor, AuditActions.UserLogin, AuditEntityTypes.User, user.Id.ToString()),
            cancellationToken);

        return Result.Success(new LoginResponse(user.ToDto(), token.Token, token.ExpiresAt));
    }
}
