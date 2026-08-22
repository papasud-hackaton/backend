using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Customers.Queries.GetCustomers;

namespace Papasur.Application.Customers.Commands.CreateCustomer;

/// <summary>Alta rápida desde el paso 1 del wizard, sin salir del formulario (contrato §3).</summary>
public sealed record CreateCustomerCommand(
    string Name,
    string TaxId,
    string CountryCode,
    string Address,
    string City,
    string? ContactName,
    string? ContactEmail,
    Actor? Actor) : ICommand<Result<CustomerDto>>;
