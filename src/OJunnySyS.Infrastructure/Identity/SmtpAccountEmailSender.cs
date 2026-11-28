using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OJunnySyS.Application.Abstractions;

namespace OJunnySyS.Infrastructure.Identity;

public sealed class SmtpAccountEmailSender(IConfiguration configuration, ILogger<SmtpAccountEmailSender> logger) : IAccountEmailSender
{
    private string? Host => configuration["Email:Smtp:Host"]?.Trim();
    private string? FromAddress => configuration["Email:Smtp:FromAddress"]?.Trim();
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    public async Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default)
        => await SendAsync(email, "Confirma tu cuenta de OJunnySyS",
            $"Confirma el correo para activar el espacio de empresa: {confirmationUrl}\r\n\r\nSi no solicitaste esta cuenta, ignora este mensaje.", cancellationToken);

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
        => await SendAsync(email, "Restablece tu contraseña de OJunnySyS",
            $"Recibimos una solicitud para cambiar la contraseña de tu cuenta: {resetUrl}\r\n\r\nSi no solicitaste este cambio, ignora este mensaje. La contraseña actual seguirá funcionando.", cancellationToken);

    public async Task SendInvitationAsync(string email, string invitationUrl, CancellationToken cancellationToken = default)
        => await SendAsync(email, "Invitación a un espacio de OJunnySyS",
            $"Te invitaron a colaborar en una empresa que utiliza OJunnySyS. Abre este enlace para confirmar tu correo y crear tu contraseña: {invitationUrl}\r\n\r\nSi no esperabas esta invitación, ignora este mensaje.", cancellationToken);

    private async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("Configura el envío de correo SMTP antes de enviar mensajes de cuenta.");
        var port = int.TryParse(configuration["Email:Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        using var message = new MailMessage
        {
            From = new MailAddress(FromAddress!, configuration["Email:Smtp:FromName"] ?? "OJunnySyS"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(email);
        using var client = new SmtpClient(Host!, port)
        {
            EnableSsl = !bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) || enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
        try { await client.SendMailAsync(message, cancellationToken); }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogError(ex, "No se pudo entregar el correo de cuenta a {Email}.", email);
            throw;
        }
    }
}
