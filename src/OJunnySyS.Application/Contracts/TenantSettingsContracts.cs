namespace OJunnySyS.Application.Contracts;

public sealed record TenantBusinessSettingsDto(string Name, string Slug, string TimeZoneId, string CurrencyCode, decimal DefaultTaxRate);
public sealed record UpdateTenantBusinessSettingsRequest(string TimeZoneId, string CurrencyCode, decimal DefaultTaxRate);
