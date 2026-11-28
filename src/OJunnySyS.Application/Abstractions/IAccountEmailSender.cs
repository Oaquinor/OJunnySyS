namespace OJunnySyS.Application.Abstractions;

public interface IAccountEmailSender
{
    bool IsConfigured { get; }
    Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
    Task SendInvitationAsync(string email, string invitationUrl, CancellationToken cancellationToken = default);
}
