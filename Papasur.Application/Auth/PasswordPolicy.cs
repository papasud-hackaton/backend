using Papasur.Application.Abstractions;

namespace Papasur.Application.Auth;

/// <summary>
/// Política de contraseñas, única para toda la app (alta, reset y cambio propio).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>Devuelve null si la contraseña es válida, o el Error de negocio si no.</summary>
    public static Error? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            return new Error(
                "User.PasswordTooShort",
                $"La contraseña debe tener al menos {MinLength} caracteres.");
        }

        return null;
    }
}
