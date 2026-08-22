using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Users.Queries.GetUsers;

/// <summary>Listado paginado de usuarios con filtros por rol, estado y texto libre.</summary>
public sealed record GetUsersQuery(
    PageRequest Page,
    string? Search = null,
    string? Role = null,
    string? Status = null) : IQuery<PagedResult<UserDto>>;
