using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Domain.Purchasing;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfWarehouseRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var warehouses = db.Warehouses.AsNoTracking();
        if (!includeInactive) warehouses = warehouses.Where(x => x.IsActive);
        return await warehouses.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<Warehouse> CreateWarehouseAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var warehouse = new Warehouse(tenantId, code, name);
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockTenantAsync(db, tenantId, cancellationToken);
        if (await db.Warehouses.AnyAsync(x => x.Code == warehouse.Code || x.Name == warehouse.Name, cancellationToken))
            throw new DomainException("Ya existe un almacén con ese código o nombre.");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var productId in products)
            db.WarehouseStocks.Add(new WarehouseStock(tenantId, warehouse.Id, productId, 0));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return warehouse;
    }

    public async Task<Warehouse> UpdateWarehouseAsync(Guid id, string name, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("El almacén no existe en esta empresa.");
        if (warehouse.Revision != expectedRevision) throw new ConcurrencyConflictException("El almacén cambió en otra sesión. Recarga la página antes de guardar.");
        warehouse.Rename(name, expectedRevision);
        if (await db.Warehouses.AnyAsync(x => x.Id != id && x.Name == warehouse.Name, cancellationToken))
            throw new DomainException("Ya existe un almacén con ese nombre.");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El almacén cambió en otra sesión. Recarga la página antes de guardar."); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un almacén con ese nombre."); }
        return warehouse;
    }

    public async Task<Warehouse> SetWarehouseActiveAsync(Guid id, bool isActive, long expectedRevision, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockTenantAsync(db, tenantId, cancellationToken);
        var warehouse = await db.Warehouses.FromSqlInterpolated($"SELECT * FROM Warehouses WHERE Id = {id} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("El almacén no existe en esta empresa.");
        if (warehouse.Revision != expectedRevision) throw new ConcurrencyConflictException("El almacén cambió en otra sesión. Recarga la página antes de guardar.");
        if (!isActive)
        {
            if (await db.PosRegisters.AnyAsync(x => x.WarehouseId == id && x.IsActive, cancellationToken))
                throw new DomainException("Asigna primero las terminales activas a otro almacén.");
            if (await db.WarehouseStocks.AnyAsync(x => x.WarehouseId == id && x.Quantity > 0, cancellationToken))
                throw new DomainException("Traslada primero las existencias antes de desactivar el almacén.");
            if (await db.PurchaseOrders.AnyAsync(x => x.WarehouseId == id && (x.Status == PurchaseStatus.Draft || x.Status == PurchaseStatus.PartiallyReceived), cancellationToken))
                throw new DomainException("Completa o cancela las órdenes pendientes asignadas a este almacén.");
            if (await db.Warehouses.CountAsync(x => x.IsActive, cancellationToken) <= 1)
                throw new DomainException("Debe quedar al menos un almacén activo.");
        }
        else
        {
            var productIds = await db.Products.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
            var existingProductIds = await db.WarehouseStocks.Where(x => x.WarehouseId == id).Select(x => x.ProductId).ToListAsync(cancellationToken);
            foreach (var productId in productIds.Except(existingProductIds))
                db.WarehouseStocks.Add(new WarehouseStock(tenantId, id, productId, 0));
        }
        warehouse.SetActive(isActive, expectedRevision);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El almacén cambió en otra sesión. Recarga la página antes de guardar."); }
        await transaction.CommitAsync(cancellationToken);
        return warehouse;
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }

    private static async Task LockTenantAsync(StoreDbContext db, Guid tenantId, CancellationToken ct) =>
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new DomainException("La empresa activa no existe.");
}
