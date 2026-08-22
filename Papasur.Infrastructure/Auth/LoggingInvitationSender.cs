using Microsoft.Extensions.Logging;
using Papasur.Application.Auth.Ports;

namespace Papasur.Infrastructure.Auth;

/// <summary>
/// Implementación de desarrollo: en vez de mandar el correo, loguea el enlace para poder
/// seguir el flujo sin servidor de mail. Reemplazar por SMTP/proveedor antes de producción
/// — el puerto ya está, sólo hay que registrar otra implementación.
/// </summary>
public sealed class LoggingInvitationSender(ILogger<LoggingInvitationSender> logger) : IInvitationSender
{
    public Task SendInvitationAsync(string email, string firstName, string token, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "INVITACIÓN para {Email}: definir contraseña con el token {Token} (no se envió correo real).",
            email,
            token);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string firstName, string token, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "RECUPERACIÓN para {Email}: token {Token} (no se envió correo real).",
            email,
            token);

        return Task.CompletedTask;
    }
}
