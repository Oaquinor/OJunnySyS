using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfTenantWorkspaceRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : ITenantWorkspaceRepository
{
    public async Task<TenantWorkspaceDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;

        var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró el espacio de empresa activo.");
        var now = DateTimeOffset.UtcNow;
        var userCount = await db.Users.CountAsync(x => x.TenantId == tenantId && x.EmailConfirmed &&
            (x.LockoutEnd == null || x.LockoutEnd <= now), cancellationToken);
        var productCount = await db.Products.CountAsync(x => x.IsActive, cancellationToken);
        var customerCount = await db.Customers.CountAsync(x => x.IsActive, cancellationToken);
        var saleCount = await db.Sales.CountAsync(cancellationToken);

        return new TenantWorkspaceDto(tenant.Name, tenant.Slug, tenant.OwnerEmail, tenant.CurrencyCode, tenant.TimeZoneId,
            tenant.PlanCode, tenant.Status, tenant.TrialEndsAtUtc, tenant.CreatedAtUtc, userCount,
            productCount, customerCount, saleCount,
            !string.IsNullOrWhiteSpace(tenant.LegalName) && !string.IsNullOrWhiteSpace(tenant.TaxIdentifier));
    }
}
