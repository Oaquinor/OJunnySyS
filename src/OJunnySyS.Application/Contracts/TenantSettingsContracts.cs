namespace OJunnySyS.Application.Contracts;

public sealed record TenantBusinessSettingsDto(string Name, string Slug, string TimeZoneId, string CurrencyCode, decimal DefaultTaxRate,
    string LegalName, string TaxIdentifier, string BusinessPhone, string BusinessAddress, string ReceiptHeader, string ReceiptFooter,
    int ReceiptPaperWidthMm, bool ReceiptShowCustomer, bool ReceiptShowSku, bool ReceiptShowTax);
public sealed record UpdateTenantBusinessSettingsRequest(string TimeZoneId, string CurrencyCode, decimal DefaultTaxRate,
    string LegalName, string TaxIdentifier, string BusinessPhone, string BusinessAddress, string ReceiptHeader, string ReceiptFooter,
    int ReceiptPaperWidthMm, bool ReceiptShowCustomer, bool ReceiptShowSku, bool ReceiptShowTax);
