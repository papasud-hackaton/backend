using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Users.Queries.GetUsers;

namespace Papasur.Application.Auth.Queries.GetCurrentUser;

/// <summary>Datos del usuario autenticado, leídos de la DB (no de los claims).</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<Result<UserDto>>;
