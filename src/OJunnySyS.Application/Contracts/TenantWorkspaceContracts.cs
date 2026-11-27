using OJunnySyS.Domain.Tenancy;

namespace OJunnySyS.Application.Contracts;

public sealed record TenantWorkspaceDto(
    string CompanyName,
    string Slug,
    string OwnerEmail,
    string CurrencyCode,
    string TimeZoneId,
    string PlanCode,
    TenantStatus Status,
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset CreatedAtUtc,
    int UserCount,
    int ActiveProductCount,
    int ActiveCustomerCount,
    int SaleCount,
    bool BusinessIdentityComplete);
