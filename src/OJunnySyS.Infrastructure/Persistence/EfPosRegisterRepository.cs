using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfPosRegisterRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IPosRegisterRepository
{
    public async Task<IReadOnlyList<PosRegister>> GetRegistersAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var registers = db.PosRegisters.AsNoTracking();
        if (!includeInactive) registers = registers.Where(x => x.IsActive);
        return await registers.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<PosRegister> CreateRegisterAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var register = new PosRegister(tenantId, code, name);
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (await db.PosRegisters.AnyAsync(x => x.Code == register.Code || x.Name == register.Name, cancellationToken))
            throw new DomainException("Ya existe un terminal con ese código o nombre.");
        db.PosRegisters.Add(register);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un terminal con ese código o nombre."); }
        return register;
    }

    public async Task<PosRegister> UpdateRegisterAsync(Guid id, string name, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var register = await db.PosRegisters.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("El terminal no existe en esta empresa.");
        if (register.Revision != expectedRevision)
            throw new ConcurrencyConflictException("El terminal cambió en otra sesión. Recarga la página antes de guardar.");
        register.Rename(name, expectedRevision);
        if (await db.PosRegisters.AnyAsync(x => x.Id != id && x.Name == register.Name, cancellationToken))
            throw new DomainException("Ya existe un terminal con ese nombre.");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El terminal cambió en otra sesión. Recarga la página antes de guardar."); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un terminal con ese nombre."); }
        return register;
    }

    public async Task<PosRegister> SetRegisterActiveAsync(Guid id, bool isActive, long expectedRevision, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var register = await db.PosRegisters.FromSqlInterpolated($"SELECT * FROM PosRegisters WHERE Id = {id} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("El terminal no existe en esta empresa.");
        if (register.Revision != expectedRevision)
            throw new ConcurrencyConflictException("El terminal cambió en otra sesión. Recarga la página antes de guardar.");
        if (!isActive)
        {
            if (await db.CashSessions.AnyAsync(x => x.RegisterCode == register.Code && x.Status == CashSessionStatus.Open, cancellationToken))
                throw new DomainException("Cierra primero el turno abierto de este terminal.");
            if (await db.PosRegisters.CountAsync(x => x.IsActive, cancellationToken) <= 1)
                throw new DomainException("Debe quedar al menos un terminal POS activo.");
        }
        register.SetActive(isActive, expectedRevision);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return register;
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }
}
