namespace Papasur.Application.Abstractions;

/// <summary>
/// Quién está ejecutando la operación, resuelto desde el JWT por el controller.
/// Viaja en los commands para que la auditoría pueda desnormalizar nombre y rol
/// (el registro histórico guarda lo que la persona era en ese momento).
/// </summary>
public sealed record Actor(Guid Id, string Name, string Role, string? IpAddress = null);
