using System.Globalization;
using System.Text;
using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Audit.Ports;

namespace Papasur.Application.Audit.Queries.GetAuditEntries;

/// <summary>
/// Arma el CSV de auditoría. Fechas en ISO 8601 y comillas escapadas: el archivo se abre
/// en Excel sin romperse aunque un detalle traiga comas o saltos de línea.
/// </summary>
public sealed class ExportAuditEntriesQueryHandler(IAuditRepository audit)
    : IQueryHandler<ExportAuditEntriesQuery, Result<string>>
{
    public async Task<Result<string>> Handle(
        ExportAuditEntriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Filter.From is { } from && query.Filter.To is { } to && from > to)
        {
            return Result.Failure<string>(new Error(
                "Audit.InvalidDateRange",
                "La fecha 'desde' no puede ser posterior a la fecha 'hasta'."));
        }

        var entries = await audit.ListAllAsync(query.Filter, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("fecha,actor,rol,accion,entidad,entidad_id,detalle,ip");

        foreach (var e in entries)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Campo(e.OccurredAt.ToString("O", CultureInfo.InvariantCulture)),
                Campo(e.ActorName),
                Campo(e.ActorRole),
                Campo(e.Action),
                Campo(e.EntityType),
                Campo(e.EntityId),
                Campo(e.Detail),
                Campo(e.IpAddress),
            }));
        }

        return Result.Success(csv.ToString());
    }

    private static string Campo(string? valor)
        => valor is null ? string.Empty : $"\"{valor.Replace("\"", "\"\"")}\"";
}
