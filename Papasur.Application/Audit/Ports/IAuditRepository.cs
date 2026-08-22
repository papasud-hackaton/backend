using Papasur.Application.Abstractions;
using Papasur.Domain.Audit;

namespace Papasur.Application.Audit.Ports;

/// <summary>
/// Filtros del listado de auditoría (contrato §6). Actions y Roles son repetibles:
/// ?action=user.login&amp;action=form.created.
/// </summary>
public sealed record AuditFilter(
    Guid? ActorId = null,
    IReadOnlyList<string>? Actions = null,
    IReadOnlyList<string>? Roles = null,
    string? EntityType = null,
    string? EntityId = null,
    string? Search = null,
    DateTime? From = null,
    DateTime? To = null);

/// <summary>
/// Puerto de auditoría. Sólo escritura desde el backend: no existe alta desde el cliente.
/// </summary>
public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<AuditEntry>> ListAsync(
        PageRequest page,
        AuditFilter filter,
        CancellationToken cancellationToken);

    /// <summary>Mismos filtros, sin paginar: alimenta la exportación a CSV.</summary>
    Task<IReadOnlyList<AuditEntry>> ListAllAsync(AuditFilter filter, CancellationToken cancellationToken);
}
