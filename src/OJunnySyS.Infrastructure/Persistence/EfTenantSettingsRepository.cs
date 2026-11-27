using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfTenantSettingsRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : ITenantSettingsRepository
{
    public async Task<TenantBusinessSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró la empresa activa.");
        return Map(tenant);
    }

    public async Task<TenantBusinessSettingsDto> UpdateAsync(UpdateTenantBusinessSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró la empresa activa.");
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        }
        catch (TimeZoneNotFoundException) { throw new DomainException("Selecciona una zona horaria disponible."); }
        catch (InvalidTimeZoneException) { throw new DomainException("La zona horaria configurada no es válida."); }
        tenant.UpdateBusinessSettings(request.TimeZoneId, request.CurrencyCode, request.DefaultTaxRate);
        tenant.UpdateReceiptSettings(request.LegalName, request.TaxIdentifier, request.BusinessPhone, request.BusinessAddress,
            request.ReceiptHeader, request.ReceiptFooter, request.ReceiptPaperWidthMm, request.ReceiptShowCustomer, request.ReceiptShowSku, request.ReceiptShowTax);
        await db.SaveChangesAsync(cancellationToken);
        return Map(tenant);
    }

    private static TenantBusinessSettingsDto Map(OJunnySyS.Domain.Tenancy.Tenant tenant) =>
        new(tenant.Name, tenant.Slug, tenant.TimeZoneId, tenant.CurrencyCode, tenant.DefaultTaxRate,
            tenant.LegalName, tenant.TaxIdentifier, tenant.BusinessPhone, tenant.BusinessAddress, tenant.ReceiptHeader, tenant.ReceiptFooter,
            tenant.ReceiptPaperWidthMm, tenant.ReceiptShowCustomer, tenant.ReceiptShowSku, tenant.ReceiptShowTax);
}
