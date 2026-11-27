using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfPosPaymentMethodRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IPosPaymentMethodRepository
{
    public async Task<IReadOnlyList<TenantPaymentMethodDto>> GetAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        var query = db.TenantPaymentMethods.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        var methods = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return methods.Select(x => x.ToDto()).ToArray();
    }

    public async Task<TenantPaymentMethodDto> CreateAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var method = new TenantPaymentMethod(tenantId, $"custom-{Guid.NewGuid():N}"[..19], request.Name, request.Type,
            request.Instructions, request.AccountLabel, request.AccountReference, request.QrPayload);
        db.TenantPaymentMethods.Add(method);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("No se pudo guardar el medio de pago. Comprueba que el nombre no esté repetido."); }
        await transaction.CommitAsync(cancellationToken);
        return method.ToDto();
    }

    public async Task<TenantPaymentMethodDto> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        var method = await db.TenantPaymentMethods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("El medio de pago no existe en esta empresa.");
        method.Update(request.Name, request.Instructions, request.AccountLabel, request.AccountReference, request.QrPayload, request.ExpectedRevision);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El medio de pago cambió en otra sesión. Recarga antes de guardar."); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un medio de pago con ese nombre."); }
        return method.ToDto();
    }

    public async Task<TenantPaymentMethodDto> SetActiveAsync(Guid id, SetPaymentMethodActiveRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var method = await db.TenantPaymentMethods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("El medio de pago no existe en esta empresa.");
        if (!request.IsActive && method.IsActive)
        {
            if (method.Type == PaymentMethodType.Cash && !await db.TenantPaymentMethods.AnyAsync(x => x.Id != id && x.IsActive && x.Type == PaymentMethodType.Cash, cancellationToken))
                throw new DomainException("Debe permanecer al menos un medio de pago en efectivo para el arqueo de caja.");
            if (!await db.TenantPaymentMethods.AnyAsync(x => x.Id != id && x.IsActive, cancellationToken))
                throw new DomainException("Debe permanecer al menos un medio de pago activo.");
        }
        method.SetActive(request.IsActive, request.ExpectedRevision);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El medio de pago cambió en otra sesión. Recarga antes de actualizarlo."); }
        await transaction.CommitAsync(cancellationToken);
        return method.ToDto();
    }
}
