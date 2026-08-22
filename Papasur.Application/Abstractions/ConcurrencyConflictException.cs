namespace Papasur.Application.Abstractions;

/// <summary>
/// Dos escrituras se pisaron en la base. La lanza Infrastructure para que Application pueda
/// responder el 409 del contrato sin conocer EF Core.
/// </summary>
public sealed class ConcurrencyConflictException(Exception inner)
    : Exception("El registro cambió mientras se guardaba.", inner);
