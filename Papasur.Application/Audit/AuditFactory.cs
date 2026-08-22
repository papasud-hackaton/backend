using Papasur.Application.Abstractions;
using Papasur.Domain.Audit;

namespace Papasur.Application.Audit;

/// <summary>
/// Arma las entradas de auditoría en un solo lugar, copiando nombre y rol del actor.
/// Ningún handler debería construir un AuditEntry a mano.
/// </summary>
public static class AuditFactory
{
    public static AuditEntry Create(
        Actor actor,
        string action,
        string entityType,
        string? entityId = null,
        string? detail = null,
        string? changes = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = actor.Id,
            ActorName = actor.Name,
            ActorRole = actor.Role,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            Changes = changes,
            IpAddress = actor.IpAddress,
            OccurredAt = DateTime.UtcNow,
        };

    /// <summary>Serializa un cambio de campo al formato que espera el front: [{field, from, to}].</summary>
    public static string ChangeSet(params (string Field, string? From, string? To)[] changes)
        => System.Text.Json.JsonSerializer.Serialize(
            changes.Select(c => new { field = c.Field, from = c.From, to = c.To }));
}
