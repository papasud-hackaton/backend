namespace Papasur.Application.Abstractions.Messaging;

/// <summary>
/// Handler de un comando CQRS. Un handler por comando; se registra scoped en DependencyInjection.cs.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}
