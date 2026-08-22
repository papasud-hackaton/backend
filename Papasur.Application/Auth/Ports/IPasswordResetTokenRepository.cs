using Papasur.Domain.Users;

namespace Papasur.Application.Auth.Ports;

/// <summary>
/// Puerto de tokens de recuperación de contraseña. Implementado en Infrastructure.
/// </summary>
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);

    /// <summary>Busca por el HASH del token (el token en claro sólo existe en el enlace).</summary>
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken);

    /// <summary>Invalida los tokens vigentes de un usuario (al pedir uno nuevo o al cambiar la clave).</summary>
    Task InvalidateAllForUserAsync(Guid userId, DateTime now, CancellationToken cancellationToken);
}
