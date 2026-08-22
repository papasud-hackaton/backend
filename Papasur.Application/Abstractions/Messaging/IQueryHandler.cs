namespace Papasur.Application.Abstractions.Messaging;

/// <summary>
/// Handler de una query CQRS. Un handler por query; se registra scoped en DependencyInjection.cs.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
