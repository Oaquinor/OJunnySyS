using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfTaxProfileRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : ITaxProfileRepository
{
    public async Task<IReadOnlyList<TaxProfile>> GetAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var (tenantId, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var query = db.TaxProfiles.AsNoTracking();
            if (!includeInactive) query = query.Where(x => x.IsActive);
            return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        }
    }

    public async Task<TaxProfile> CreateAsync(CreateTaxProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("La empresa activa no existe.");
            var profile = new TaxProfile(tenantId, request.Code, request.Name, request.Rate);
            db.TaxProfiles.Add(profile);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { throw new DomainException("Ya existe un impuesto con ese código en esta empresa."); }
            await transaction.CommitAsync(cancellationToken);
            return profile;
        }
    }

    public async Task<TaxProfile> UpdateAsync(Guid id, UpdateTaxProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var profile = await db.TaxProfiles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new DomainException("El perfil de impuesto no existe en esta empresa.");
            profile.Update(request.Name, request.Rate, request.ExpectedRevision);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El impuesto cambió en otra sesión. Recarga antes de guardar."); }
            return profile;
        }
    }

    public async Task<TaxProfile> SetActiveAsync(Guid id, SetTaxProfileActiveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (_, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var profile = await db.TaxProfiles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new DomainException("El perfil de impuesto no existe en esta empresa.");
            if (!request.IsActive && profile.IsActive && await db.Products.AnyAsync(x => x.IsActive && x.TaxProfileId == id, cancellationToken))
                throw new DomainException("Reasigna este impuesto a los productos activos antes de desactivarlo.");
            profile.SetActive(request.IsActive, request.ExpectedRevision);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El impuesto cambió en otra sesión. Recarga antes de actualizarlo."); }
            return profile;
        }
    }

    private async Task<(Guid TenantId, StoreDbContext Context)> OpenTenantContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return (tenantId, db);
    }
}
