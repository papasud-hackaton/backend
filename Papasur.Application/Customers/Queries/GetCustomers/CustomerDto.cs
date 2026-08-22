namespace Papasur.Application.Customers.Queries.GetCustomers;

/// <summary>
/// Cliente / importador en el shape del contrato §3. El nombre del país viaja resuelto porque
/// los documentos lo imprimen y el front no tiene la tabla de países.
/// </summary>
public sealed record CustomerDto(
    Guid Id,
    string Name,
    string TaxId,
    string CountryCode,
    string CountryName,
    string Address,
    string City,
    string? ContactName,
    string? ContactEmail,
    string? DefaultIncoterm,
    string? DefaultPortOfDischarge);
