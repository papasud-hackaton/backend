namespace Papasur.Application.Users.Queries.GetUsers;

/// <summary>
/// Usuario tal como lo consume el front (contrato §2). NUNCA expone el PasswordHash.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeId,
    string Role,
    string Status,
    string? Phone,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
