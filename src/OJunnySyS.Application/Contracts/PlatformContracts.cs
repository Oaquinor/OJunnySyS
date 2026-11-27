using OJunnySyS.Domain.Tenancy;

namespace OJunnySyS.Application.Contracts;

public sealed record CreateTenantRequest(string Name, string Slug, string OwnerEmail, string TimeZoneId, string CurrencyCode,
    string AdministratorName, string AdministratorEmail, string InitialPassword);
public sealed record TenantSummary(Guid Id, string Name, string Slug, string OwnerEmail, string TimeZoneId,
    string CurrencyCode, string PlanCode, TenantStatus Status, DateTimeOffset? TrialEndsAtUtc, DateTimeOffset CreatedAtUtc, int UserCount);
