using OJunnySyS.Domain.Tenancy;

namespace OJunnySyS.Application.Abstractions;

public interface IPlatformManagementRepository
{
    Task<IReadOnlyList<(Tenant Tenant, int UserCount)>> GetTenantsAsync(CancellationToken cancellationToken = default);
    Task<Tenant> CreateTenantAsync(Tenant tenant, string administratorName, string administratorEmail, string initialPassword, CancellationToken cancellationToken = default);
    Task<Tenant> UpdateTenantStatusAsync(Guid tenantId, TenantStatus status, string? planCode = null, CancellationToken cancellationToken = default);
}
