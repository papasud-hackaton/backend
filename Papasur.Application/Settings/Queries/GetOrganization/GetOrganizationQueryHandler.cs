using System.Text.Json;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Settings.Ports;

namespace Papasur.Application.Settings.Queries.GetOrganization;

public sealed class GetOrganizationQueryHandler(IOrganizationRepository organization)
    : IQueryHandler<GetOrganizationQuery, IReadOnlyDictionary<string, string>>
{
    public async Task<IReadOnlyDictionary<string, string>> Handle(
        GetOrganizationQuery query,
        CancellationToken cancellationToken)
    {
        var settings = await organization.GetAsync(cancellationToken);

        return OrganizationValues.Parse(settings.ValuesJson);
    }
}

/// <summary>Serialización del mapa clave/valor, compartida por la query y el command.</summary>
public static class OrganizationValues
{
    public static IReadOnlyDictionary<string, string> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    public static string Serialize(IReadOnlyDictionary<string, string> values)
        => JsonSerializer.Serialize(values);
}
