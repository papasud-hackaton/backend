using Papasur.Application.Customers.Queries.GetCustomers;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Customers;

/// <summary>Traducción entidad → contrato en un solo lugar.</summary>
public static class CustomerMapping
{
    public static CustomerDto ToDto(this Cliente customer) => new(
        customer.Id,
        customer.Nombre,
        customer.TaxId ?? string.Empty,
        customer.CountryCode ?? string.Empty,
        customer.Pais ?? Countries.NameOf(customer.CountryCode),
        customer.Address ?? string.Empty,
        customer.City ?? string.Empty,
        customer.ContactName,
        customer.ContactEmail,
        customer.DefaultIncoterm,
        customer.DefaultPortOfDischarge);
}

/// <summary>
/// Países de destino que hoy usa la operación. Cuando crezca, sale de una tabla; mientras
/// tanto vive acá y no se inventa: si el código no está, se devuelve el código.
/// </summary>
public static class Countries
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AR"] = "Argentina",
        ["BR"] = "Brasil",
        ["PY"] = "Paraguay",
        ["UY"] = "Uruguay",
        ["BO"] = "Bolivia",
        ["CL"] = "Chile",
        ["VE"] = "Venezuela",
        ["ES"] = "España",
        ["IT"] = "Italia",
        ["VN"] = "Vietnam",
    };

    public static string NameOf(string? code)
        => string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : Names.TryGetValue(code, out var name) ? name : code;
}
