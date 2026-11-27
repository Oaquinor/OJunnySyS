using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Tenancy;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class TenantAccessGuard(IDbContextFactory<StoreDbContext> contextFactory, IBusinessClock clock) : ITenantAccessGuard
{
    public async Task EnsureCanOperateAsync(Guid tenantId, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new TenantAccessException("La empresa activa no es válida.");
        if (userId is null || userId == Guid.Empty) throw new TenantAccessException("No se pudo validar la sesión de usuario.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new TenantAccessException("No se encontró el espacio de empresa asociado a esta cuenta.");
        tenant.EnsureCanOperate(clock.UtcNow);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, cancellationToken)
            ?? throw new TenantAccessException("La cuenta ya no pertenece a esta empresa.");
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > clock.UtcNow)
            throw new TenantAccessException("El acceso de esta cuenta está desactivado o bloqueado.");
    }
}
