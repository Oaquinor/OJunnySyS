namespace OJunnySyS.Application.Abstractions;

public interface IRegistrationEmailSender
{
    bool IsConfigured { get; }
    Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default);
}
