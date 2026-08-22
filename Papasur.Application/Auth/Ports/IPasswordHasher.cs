namespace Papasur.Application.Auth.Ports;

/// <summary>
/// Hashing de contraseñas. La implementación (Infrastructure) usa PBKDF2-SHA256 con salt
/// por usuario; Application no conoce el algoritmo.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Verificación en tiempo constante contra el hash almacenado.</summary>
    bool Verify(string password, string passwordHash);
}
