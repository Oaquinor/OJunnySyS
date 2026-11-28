using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfPosOfflineDeviceRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IPosOfflineDeviceRepository
{
    public async Task<IReadOnlyList<PosOfflineDevice>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var db = await CreateDbContextAsync(cancellationToken);
        await using (db)
            return await db.PosOfflineDevices.AsNoTracking().Where(x => x.UserId == GetUserId())
                .OrderByDescending(x => x.IsActive).ThenBy(x => x.Label).ToListAsync(cancellationToken);
    }

    public async Task<PosOfflineDevice> RegisterAsync(Guid deviceId, string label, string publicKey, string fingerprint, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var userId = GetUserId();
        var db = await CreateDbContextAsync(cancellationToken);
        await using (db)
        {
            var existing = await db.PosOfflineDevices.SingleOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
            if (existing is not null)
            {
                if (existing.UserId == userId && existing.IsActive && existing.Fingerprint == fingerprint)
                    return existing;
                throw new DomainException("Este identificador de dispositivo ya está inscrito o fue revocado. Genera una identidad nueva en este navegador.");
            }
            if (await db.PosOfflineDevices.AnyAsync(x => x.UserId == userId && x.Fingerprint == fingerprint, cancellationToken))
                throw new DomainException("Esta clave ya pertenece a un dispositivo inscrito. Actualiza la identidad local e inténtalo de nuevo.");
            var activeCount = await db.PosOfflineDevices.CountAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
            if (activeCount >= 5) throw new DomainException("Este usuario ya tiene cinco dispositivos POS activos. Revoca uno antes de inscribir otro.");
            var device = new PosOfflineDevice(tenantId, userId, deviceId, label, publicKey, fingerprint);
            db.PosOfflineDevices.Add(device);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { throw new DomainException("No se pudo inscribir el dispositivo. Actualiza la lista e inténtalo de nuevo."); }
            return device;
        }
    }

    public async Task RevokeAsync(Guid deviceId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var userId = GetUserId();
        var db = await CreateDbContextAsync(cancellationToken);
        await using (db)
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var device = await db.PosOfflineDevices.FromSqlInterpolated($"SELECT * FROM PosOfflineDevices WHERE TenantId = {tenantId} AND UserId = {userId} AND DeviceId = {deviceId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken) ?? throw new DomainException("El dispositivo no existe en esta sesión.");
            device.Revoke(expectedRevision);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El dispositivo cambió en otra sesión. Actualiza la lista."); }
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<PosOfflineDevice?> GetActiveForAttestationAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var db = await CreateDbContextAsync(cancellationToken);
        await using (db)
            return await db.PosOfflineDevices.AsNoTracking().SingleOrDefaultAsync(x => x.DeviceId == deviceId && x.UserId == userId && x.IsActive, cancellationToken);
    }

    private Guid GetUserId() => tenantContext.CurrentUserId is { } id && id != Guid.Empty
        ? id
        : throw new DomainException("No se pudo identificar al usuario actual.");

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }
}
