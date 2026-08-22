namespace Papasur.Application.Abstractions.Messaging;

/// <summary>
/// Marcador de una query CQRS (operación de lectura, sin efectos) que produce <typeparamref name="TResponse"/>.
/// </summary>
public interface IQuery<TResponse>;
