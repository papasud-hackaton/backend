using Microsoft.AspNetCore.Mvc;
using Papasur.Api.Authorization;
using Papasur.Api.Contracts;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Commands.CreateUser;
using Papasur.Application.Users.Commands.DeactivateUser;
using Papasur.Application.Users.Commands.SetUserStatus;
using Papasur.Application.Users.Commands.UpdateUser;
using Papasur.Application.Users.Queries.GetUserById;
using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Users;

namespace Papasur.Api.Controllers;

/// <summary>
/// Administración de usuarios (contrato §2). Todo exige users.manage → SOLO admin.
/// El alta es siempre manual y por invitación: no existe registro público.
/// </summary>
[Route("api/v1/users")]
[AuthorizeRoles(RoleNames.Admin)]
public class UsersController : ApiControllerBase
{
    /// <summary>Listado paginado. search busca en nombre, apellido, correo y legajo.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromServices] IQueryHandler<GetUsersQuery, PagedResult<UserDto>> handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null)
    {
        var result = await handler.Handle(
            new GetUsersQuery(new PageRequest(page, pageSize), search, role, status),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetUserByIdQuery, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetUserByIdQuery(id), cancellationToken);

        return result.IsFailure
            ? Fail(StatusCodes.Status404NotFound, result.Error)
            : Ok(result.Value);
    }

    /// <summary>
    /// Alta de usuario. No lleva contraseña: se crea "invited" y el backend manda la invitación.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] ICommandHandler<CreateUserCommand, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateUserCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.EmployeeId,
                request.Role,
                request.Phone)
            {
                Actor = CurrentActor,
            },
            cancellationToken);

        if (result.IsFailure)
        {
            return Fail(StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Edición parcial. Un cambio de rol se audita con el valor anterior y el nuevo.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        [FromServices] ICommandHandler<UpdateUserCommand, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateUserCommand(id)
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Role = request.Role,
                Actor = CurrentActor,
            },
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "User.NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Fail(status, result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>Reactiva un usuario dado de baja.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<UserDto>> Activate(
        Guid id,
        [FromServices] ICommandHandler<SetUserStatusCommand, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SetUserStatusCommand(id, UserStatuses.Active) { Actor = CurrentActor },
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "User.NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Fail(status, result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>Baja lógica: no hay DELETE, los formularios históricos conservan su autor.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<UserDto>> Deactivate(
        Guid id,
        [FromServices] ICommandHandler<DeactivateUserCommand, Result<UserDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new DeactivateUserCommand(id) { Actor = CurrentActor },
            cancellationToken);

        if (result.IsFailure)
        {
            var status = result.Error.Code == "User.NotFound"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;

            return Fail(status, result.Error);
        }

        return Ok(result.Value);
    }
}

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string EmployeeId,
    string Role,
    string? Phone = null);

public sealed record UpdateUserRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Role = null);
