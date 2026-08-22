using Microsoft.AspNetCore.Mvc;
using Papasur.Application.Abstractions;

namespace Papasur.Api.Contracts;

/// <summary>
/// Forma ÚNICA de error de la API (contrato §0): toda respuesta que no sea 2xx devuelve
/// { message, code }. El message se muestra tal cual al usuario, en español; el code es
/// para que el front decida (por ejemplo "version_conflict" o "token_expired").
/// </summary>
public sealed record ApiError(string Message, string? Code = null);

/// <summary>Helpers para mapear un Error de negocio a la respuesta HTTP correspondiente.</summary>
public static class ApiErrorResults
{
    public static ObjectResult Result(int statusCode, string message, string? code = null)
        => new(new ApiError(message, code)) { StatusCode = statusCode };

    /// <summary>Convierte un Error del Result pattern: el mensaje va al usuario, el código a la máquina.</summary>
    public static ObjectResult FromError(int statusCode, Error error)
        => Result(statusCode, error.Message, ToSnakeCase(error.Code));

    /// <summary>"User.EmailAlreadyExists" -> "user_email_already_exists" (códigos estables para el front).</summary>
    private static string ToSnakeCase(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var chars = new List<char>(code.Length + 8);

        foreach (var c in code)
        {
            if (c == '.')
            {
                chars.Add('_');
                continue;
            }

            if (char.IsUpper(c) && chars.Count > 0 && chars[^1] != '_')
            {
                chars.Add('_');
            }

            chars.Add(char.ToLowerInvariant(c));
        }

        return new string([.. chars]);
    }
}
