using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;

namespace Papasur.Application.Audit.Queries.GetAuditEntries;

/// <summary>Exportación CSV de la auditoría: mismos filtros que el listado, sin paginar.</summary>
public sealed record ExportAuditEntriesQuery(AuditFilter Filter) : IQuery<Result<string>>;
