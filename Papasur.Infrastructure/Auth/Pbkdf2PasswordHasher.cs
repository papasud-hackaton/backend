using System.Security.Cryptography;
using Papasur.Application.Auth.Ports;

namespace Papasur.Infrastructure.Auth;

/// <summary>
/// Hashing PBKDF2-SHA256 con salt aleatorio por usuario (sin dependencias externas).
/// Formato almacenado: "iteraciones.saltBase64.hashBase64".
/// La verificación es en tiempo constante (CryptographicOperations.FixedTimeEquals).
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;

    private const int SaltSize = 16;

    private const int KeySize = 32;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('.', 3);

        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
