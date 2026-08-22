using Papasur.Application.Abstractions;
using Papasur.Domain.Audit;

namespace Papasur.Application.Audit.Ports;

/// <summary>Filtros del listado de auditoría (todos opcionales y combinables).</summary>
public sealed record AuditFilter(
    Guid? UserId = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    DateTime? From = null,
    DateTime? To = null);

/// <summary>
/// Puerto de auditoría. Implementado en Infrastructure (EfAuditRepository).
/// </summary>
public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<AuditEntry>> ListAsync(
        PageRequest page,
        AuditFilter filter,
        CancellationToken cancellationToken);
}
