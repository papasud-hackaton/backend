using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Customers.Ports;
using Papasur.Application.Customers.Queries.GetCustomers;
using Papasur.Domain.Trazabilidad;

namespace Papasur.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(ICustomerRepository customers)
    : ICommandHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CustomerDto>(new Error("Customer.NameRequired", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(command.CountryCode))
        {
            return Result.Failure<CustomerDto>(new Error("Customer.CountryRequired", "El país es obligatorio."));
        }

        if (await customers.NameExistsAsync(name, cancellationToken))
        {
            return Result.Failure<CustomerDto>(new Error("Customer.AlreadyExists", "Ya existe un cliente con ese nombre."));
        }

        var countryCode = command.CountryCode.Trim().ToUpperInvariant();

        var customer = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = name,
            TaxId = Blank(command.TaxId),
            CountryCode = countryCode,
            Pais = Countries.NameOf(countryCode),
            Address = Blank(command.Address),
            City = Blank(command.City),
            ContactName = Blank(command.ContactName),
            ContactEmail = Blank(command.ContactEmail),
        };

        await customers.AddAsync(customer, cancellationToken);

        return Result.Success(customer.ToDto());

        static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
