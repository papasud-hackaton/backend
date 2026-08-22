using Papasur.Application.Users.Queries.GetUsers;
using Papasur.Domain.Users;

namespace Papasur.Application.Users.Mapping;

/// <summary>Proyección única de User a DTO: un solo lugar que decide qué sale hacia afuera.</summary>
public static class UserMapping
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.Email,
        user.EmployeeId,
        user.Role?.Name ?? string.Empty,
        user.Status,
        user.Phone,
        user.CreatedAt,
        user.LastLoginAt);
}
