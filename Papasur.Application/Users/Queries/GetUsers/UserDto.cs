namespace Papasur.Application.Users.Queries.GetUsers;

/// <summary>Proyección de usuario hacia afuera: nunca expone el PasswordHash.</summary>
public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string EmployeeNumber,
    int RoleId,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
