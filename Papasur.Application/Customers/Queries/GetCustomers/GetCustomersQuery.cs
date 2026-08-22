using Papasur.Application.Abstractions;
using Papasur.Application.Abstractions.Messaging;

namespace Papasur.Application.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery(string? Search) : IQuery<Result<IReadOnlyList<CustomerDto>>>;
