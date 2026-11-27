using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OJunnySyS.Application.Abstractions;

namespace OJunnySyS.Infrastructure.Identity;

public sealed class SmtpRegistrationEmailSender(IConfiguration configuration, ILogger<SmtpRegistrationEmailSender> logger) : IRegistrationEmailSender
{
    private string? Host => configuration["Email:Smtp:Host"]?.Trim();
    private string? FromAddress => configuration["Email:Smtp:FromAddress"]?.Trim();
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    public async Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("Configura el envío de correo SMTP antes de enviar confirmaciones.");
        var port = int.TryParse(configuration["Email:Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        using var message = new MailMessage
        {
            From = new MailAddress(FromAddress!, configuration["Email:Smtp:FromName"] ?? "OJunnySyS"),
            Subject = "Confirma tu cuenta de OJunnySyS",
            Body = $"Confirma el correo para activar el espacio de empresa: {confirmationUrl}\r\n\r\nSi no solicitaste esta cuenta, ignora este mensaje.",
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
            logger.LogError(ex, "No se pudo entregar el correo de confirmación a {Email}.", email);
            throw;
        }
    }
}
