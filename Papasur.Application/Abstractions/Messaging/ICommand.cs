namespace Papasur.Application.Abstractions.Messaging;

/// <summary>
/// Marcador de un comando CQRS (operación de escritura) que produce <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommand<TResponse>;
