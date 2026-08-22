namespace Papasur.Application.Audit.Queries.GetAuditEntries;

/// <summary>
/// Fila de auditoría con los datos del agente resueltos (para no obligar al front a
/// pedir cada usuario por separado).
/// </summary>
public sealed record AuditEntryDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    string UserEmployeeNumber,
    string Action,
    string EntityType,
    string? EntityId,
    string? Detail,
    string? IpAddress,
    DateTime OccurredAt);
