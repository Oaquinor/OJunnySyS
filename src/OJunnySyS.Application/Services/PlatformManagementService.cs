using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Tenancy;

namespace OJunnySyS.Application.Services;

public sealed class PlatformManagementService(IPlatformManagementRepository repository)
{
    public async Task<IReadOnlyList<TenantSummary>> GetTenantsAsync(CancellationToken ct = default) =>
        (await repository.GetTenantsAsync(ct)).Select(x => Map(x.Tenant, x.UserCount)).ToArray();

    public async Task<TenantSummary> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.Equals(request.OwnerEmail.Trim(), request.AdministratorEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new OJunnySyS.Domain.Common.DomainException("El correo responsable y el correo administrador deben coincidir para el alta inicial.");
        var tenant = new Tenant(request.Name, request.Slug, request.OwnerEmail, request.TimeZoneId, request.CurrencyCode);
        var saved = await repository.CreateTenantAsync(tenant, request.AdministratorName, request.AdministratorEmail, request.InitialPassword, ct);
        return Map(saved, 1);
    }

    public async Task<TenantSummary> SetTenantStatusAsync(Guid tenantId, bool active, string? planCode = null, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty) throw new OJunnySyS.Domain.Common.DomainException("Selecciona una empresa válida.");
        var tenant = await repository.UpdateTenantStatusAsync(tenantId, active ? TenantStatus.Active : TenantStatus.Suspended, planCode, ct);
        return Map(tenant, 0);
    }

    private static TenantSummary Map(Tenant tenant, int userCount) => new(tenant.Id, tenant.Name, tenant.Slug, tenant.OwnerEmail,
        tenant.TimeZoneId, tenant.CurrencyCode, tenant.PlanCode, tenant.Status, tenant.TrialEndsAtUtc, tenant.CreatedAtUtc, userCount);
}
