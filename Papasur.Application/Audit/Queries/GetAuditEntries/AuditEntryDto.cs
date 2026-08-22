namespace Papasur.Application.Audit.Queries.GetAuditEntries;

/// <summary>Un cambio puntual de campo, tal como lo renderiza la pantalla de auditoría.</summary>
public sealed record AuditFieldChange(string Field, string? From, string? To);

/// <summary>
/// Entrada de auditoría tal como la consume el front (contrato §6). actorName y actorRole
/// salen de la propia entrada, no del usuario actual: es el histórico de lo que la persona
/// era en ese momento.
/// </summary>
public sealed record AuditEntryDto(
    Guid Id,
    Guid ActorId,
    string ActorName,
    string ActorRole,
    string Action,
    string EntityType,
    string? EntityId,
    string EntityLabel,
    IReadOnlyList<AuditFieldChange>? Changes,
    string? IpAddress,
    DateTime CreatedAt);
