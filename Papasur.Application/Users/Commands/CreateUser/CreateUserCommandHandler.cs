using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Auth.Ports;
using Papasur.Application.Roles.Ports;
using Papasur.Application.Users.Ports;
using Papasur.Domain.Audit;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IAuditRepository audit)
    : ICommandHandler<CreateUserCommand, Result<Guid>>
{
    public const int MinPasswordLength = 8;

    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<Guid>(new Error("User.NameRequired", "El nombre es obligatorio."));
        }

        var email = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!IsValidEmail(email))
        {
            return Result.Failure<Guid>(new Error("User.EmailInvalid", "El correo no es válido."));
        }

        if (string.IsNullOrWhiteSpace(command.EmployeeNumber))
        {
            return Result.Failure<Guid>(new Error("User.EmployeeNumberRequired", "El legajo es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < MinPasswordLength)
        {
            return Result.Failure<Guid>(new Error(
                "User.PasswordTooShort",
                $"La contraseña debe tener al menos {MinPasswordLength} caracteres."));
        }

        if (!await roles.ExistsAsync(command.RoleId, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("User.RoleNotFound", "El rol indicado no existe."));
        }

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("User.EmailAlreadyExists", "Ya existe un usuario con ese correo."));
        }

        var employeeNumber = command.EmployeeNumber.Trim();

        if (await users.EmployeeNumberExistsAsync(employeeNumber, cancellationToken))
        {
            return Result.Failure<Guid>(new Error(
                "User.EmployeeNumberAlreadyExists",
                "Ya existe un usuario con ese legajo."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(command.Password),
            EmployeeNumber = employeeNumber,
            RoleId = command.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await users.AddAsync(user, cancellationToken);

        // La auditoría se atribuye a quien ejecutó el alta; si no hay JWT (seed inicial), al propio usuario.
        await audit.AddAsync(
            new AuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = command.PerformedByUserId ?? user.Id,
                Action = AuditActions.UserCreated,
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                Detail = $"Alta de {user.Email} (legajo {user.EmployeeNumber}).",
                IpAddress = command.IpAddress,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Result.Success(user.Id);
    }

    private static bool IsValidEmail(string email)
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
