using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Papasur.Api.Middleware;

/// <summary>
/// Última línea de defensa: cualquier excepción no manejada se loguea y sale como
/// 500 ProblemDetails (RFC 7807) sin filtrar detalles internos. Los errores de negocio
/// esperables NO deberían llegar acá: se modelan con Result en Application.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Excepción no manejada en {Path}", httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno del servidor",
            Detail = "Ocurrió un error inesperado. Reintentá más tarde.",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
