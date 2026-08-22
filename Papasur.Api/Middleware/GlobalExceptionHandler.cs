using Microsoft.AspNetCore.Diagnostics;
using Papasur.Api.Contracts;

namespace Papasur.Api.Middleware;

/// <summary>
/// Último recinto para lo inesperado: cualquier excepción no manejada se convierte en un 500
/// con la MISMA forma de error que el resto de la API ({ message, code }), sin filtrar detalles
/// internos al cliente. Lo esperable se maneja con el Result pattern, no con excepciones.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Error no manejado en {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new ApiError("Ocurrió un error inesperado. Volvé a intentar en unos minutos.", "internal_error"),
            cancellationToken);

        return true;
    }
}
