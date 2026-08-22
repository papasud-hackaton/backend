namespace Papasur.Application.Auth.Ports;

/// <summary>
/// Envío de correos de invitación y recuperación. Detrás de un puerto para que Application
/// no sepa si es SMTP, un proveedor externo o (en desarrollo) sólo un log.
/// </summary>
public interface IInvitationSender
{
    /// <summary>Invitación a definir la contraseña por primera vez (alta de usuario).</summary>
    Task SendInvitationAsync(string email, string firstName, string token, CancellationToken cancellationToken);

    /// <summary>Enlace de recuperación de contraseña.</summary>
    Task SendPasswordResetAsync(string email, string firstName, string token, CancellationToken cancellationToken);
}
