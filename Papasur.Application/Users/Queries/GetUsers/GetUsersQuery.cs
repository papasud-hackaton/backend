using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Users.Queries.GetUsers;

/// <summary>Listado paginado de usuarios con filtros opcionales.</summary>
public sealed record GetUsersQuery(
    PageRequest Page,
    string? Search = null,
    int? RoleId = null,
    bool? IsActive = null) : IQuery<PagedResult<UserDto>>;
