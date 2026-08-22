using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;

namespace Papasur.Application.Audit.Queries.GetAuditEntries;

/// <summary>Consulta paginada de auditoría con filtros combinables (ver AuditFilter).</summary>
public sealed record GetAuditEntriesQuery(PageRequest Page, AuditFilter Filter)
    : IQuery<Result<PagedResult<AuditEntryDto>>>;
