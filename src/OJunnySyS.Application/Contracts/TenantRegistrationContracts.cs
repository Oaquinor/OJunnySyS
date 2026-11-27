namespace OJunnySyS.Application.Contracts;

public sealed record TenantRegistrationRequest(string CompanyName, string OwnerName, string Email, string Password,
    string TimeZoneId = "America/Santo_Domingo", string CurrencyCode = "DOP");

public sealed record TenantRegistrationResult(Guid UserId, string Email, string CompanyName, string ConfirmationToken);

public sealed record RegistrationSubmittedViewModel(string CompanyName, string? DevelopmentConfirmationUrl, string Message);
