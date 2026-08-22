using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;
using Papasur.Application.Customers.Ports;

namespace Papasur.Application.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(ICustomerRepository customers)
    : IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerDto>>>
{
    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(
        GetCustomersQuery query,
        CancellationToken cancellationToken)
    {
        var items = await customers.ListAsync(query.Search, cancellationToken);

        return Result.Success<IReadOnlyList<CustomerDto>>([.. items.Select(c => c.ToDto())]);
    }
}
