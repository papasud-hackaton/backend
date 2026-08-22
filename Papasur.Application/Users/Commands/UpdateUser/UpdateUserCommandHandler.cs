using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit;
using Papasur.Application.Audit.Ports;
using Papasur.Application.Roles.Ports;
using Papasur.Application.Users.Mapping;
using Papasur.Application.Users.Ports;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Audit;

namespace Papasur.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IAuditRepository audit)
    : ICommandHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(new Error("User.NotFound", "El usuario no existe."));
        }

        var rolAnterior = user.Role?.Name;
        var cambioDeRol = false;

        if (command.FirstName is { } firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                return Result.Failure<UserDto>(new Error("User.FirstNameRequired", "El nombre es obligatorio."));
            }

            user.FirstName = firstName.Trim();
        }

        if (command.LastName is { } lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
            {
                return Result.Failure<UserDto>(new Error("User.LastNameRequired", "El apellido es obligatorio."));
            }

            user.LastName = lastName.Trim();
        }

        if (command.Phone is { } phone)
        {
            user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }

        if (command.Role is { } roleName && !string.IsNullOrWhiteSpace(roleName))
        {
            var role = await roles.GetByNameAsync(roleName.Trim().ToLowerInvariant(), cancellationToken);

            if (role is null)
            {
                return Result.Failure<UserDto>(new Error("User.RoleNotFound", "El rol indicado no existe."));
            }

            if (role.Id != user.RoleId)
            {
                user.RoleId = role.Id;
                user.Role = role;
                cambioDeRol = true;
            }
        }

        await users.UpdateAsync(user, cancellationToken);

        if (command.Actor is { } actor)
        {
            // Un cambio de rol se audita aparte y con el valor anterior: es la acción sensible.
            if (cambioDeRol)
            {
                await audit.AddAsync(
                    AuditFactory.Create(
                        actor,
                        AuditActions.UserRoleChanged,
                        AuditEntityTypes.User,
                        user.Id.ToString(),
                        $"Rol de {user.Email}: {rolAnterior} → {user.Role.Name}.",
                        AuditFactory.ChangeSet(("role", rolAnterior, user.Role.Name))),
                    cancellationToken);
            }

            await audit.AddAsync(
                AuditFactory.Create(
                    actor,
                    AuditActions.UserUpdated,
                    AuditEntityTypes.User,
                    user.Id.ToString()),
                cancellationToken);
        }

        return Result.Success(user.ToDto());
    }
}
