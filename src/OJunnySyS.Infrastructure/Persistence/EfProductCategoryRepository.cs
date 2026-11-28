using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfProductCategoryRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IProductCategoryRepository
{
    public async Task<IReadOnlyList<ProductCategory>> GetProductCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var (_, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            IQueryable<ProductCategory> query = db.ProductCategories.AsNoTracking().Include(x => x.ParentCategory);
            if (!includeInactive) query = query.Where(x => x.IsActive);
            return await query.OrderBy(x => x.ParentCategoryId).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        }
    }

    public async Task<ProductCategory> CreateProductCategoryAsync(string name, Guid? parentCategoryId = null, CancellationToken cancellationToken = default)
    {
        var (tenantId, db) = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("La empresa activa no existe.");

            ProductCategory? parent = null;
            if (parentCategoryId is { } parentId)
            {
                parent = await db.ProductCategories.SingleOrDefaultAsync(x => x.Id == parentId && x.IsActive, cancellationToken)
                    ?? throw new DomainException("El grupo seleccionado no existe o está inactivo.");
                if (parent.ParentCategoryId is not null)
                    throw new DomainException("Los subgrupos no admiten más niveles de anidación.");
                if (parent.Name.Length + 3 + (name ?? string.Empty).Trim().Length > 80)
                    throw new DomainException("El nombre del grupo y subgrupo supera el máximo admitido para una clasificación de producto.");
            }

            var category = new ProductCategory(tenantId, name ?? string.Empty, parentCategoryId);
            db.ProductCategories.Add(category);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { throw new DomainException("Ya existe un grupo o subgrupo con ese nombre."); }

            category = await db.ProductCategories.Include(x => x.ParentCategory).SingleAsync(x => x.Id == category.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return category;
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
