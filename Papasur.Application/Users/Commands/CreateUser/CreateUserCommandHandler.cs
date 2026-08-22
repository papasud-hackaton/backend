using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Commands.ForgotPassword;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Roles.Ports;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordResetTokenRepository tokens,
    IInvitationSender sender,
    IAuditRepository audit)
    : ICommandHandler<CreateUserCommand, Result<UserDto>>
{
    /// <summary>La invitación dura más que una recuperación: el alta puede tardar en atenderse.</summary>
    public static readonly TimeSpan VigenciaInvitacion = TimeSpan.FromDays(7);

    public async Task<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return Result.Failure<UserDto>(new Error("User.FirstNameRequired", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return Result.Failure<UserDto>(new Error("User.LastNameRequired", "El apellido es obligatorio."));
        }

        var email = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!EmailValidator.IsValid(email))
        {
            return Result.Failure<UserDto>(new Error("User.EmailInvalid", "El correo no es válido."));
        }

        if (string.IsNullOrWhiteSpace(command.EmployeeId))
        {
            return Result.Failure<UserDto>(new Error("User.EmployeeIdRequired", "El legajo es obligatorio."));
        }

        var role = await roles.GetByNameAsync(command.Role?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);

        if (role is null)
        {
            return Result.Failure<UserDto>(new Error("User.RoleNotFound", "El rol indicado no existe."));
        }

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            return Result.Failure<UserDto>(new Error("User.EmailAlreadyExists", "Ya existe un usuario con ese correo."));
        }

        var employeeId = command.EmployeeId.Trim();

        if (await users.EmployeeIdExistsAsync(employeeId, cancellationToken))
        {
            return Result.Failure<UserDto>(new Error("User.EmployeeIdAlreadyExists", "Ya existe un usuario con ese legajo."));
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Email = email,
            // Sin contraseña: se define desde la invitación.
            PasswordHash = string.Empty,
            EmployeeId = employeeId,
            Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim(),
            RoleId = role.Id,
            Role = role,
            Status = UserStatuses.Invited,
            CreatedAt = now,
        };

        await users.AddAsync(user, cancellationToken);

        var invitacion = ResetTokens.NewToken();

        await tokens.AddAsync(
            new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ResetTokens.Hash(invitacion),
                ExpiresAt = now.Add(VigenciaInvitacion),
                CreatedAt = now,
            },
            cancellationToken);

        await sender.SendInvitationAsync(user.Email, user.FirstName, invitacion, cancellationToken);

        if (command.Actor is { } actor)
        {
            await audit.AddAsync(
                AuditFactory.Create(
                    actor,
                    AuditActions.UserCreated,
                    AuditEntityTypes.User,
                    user.Id.ToString(),
                    $"Alta de {user.Email} ({role.Name})."),
                cancellationToken);
        }

        return Result.Success(user.ToDto());
    }
}

/// <summary>Validación de correo compartida por las altas y ediciones.</summary>
public static class EmailValidator
{
    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var at = email.IndexOf('@');

        return at > 0
            && at == email.LastIndexOf('@')
            && at < email.Length - 1
            && email.IndexOf('.', at) > at + 1
            && !email.EndsWith('.')
            && !email.Contains(' ');
    }
}
