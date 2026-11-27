using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ITenantOnboardingService
{
    Task<TenantRegistrationResult> RegisterAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<string?> GenerateConfirmationTokenAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
