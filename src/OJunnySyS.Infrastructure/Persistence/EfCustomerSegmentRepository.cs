using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfCustomerSegmentRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess,
    IBusinessClock clock) : ICustomerSegmentRepository
{
    public async Task<IReadOnlyList<CustomerSegmentDto>> GetSegmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await (from segment in db.CustomerSegments.AsNoTracking()
                      join tag in db.CustomerTags.AsNoTracking() on segment.RequiredTagId equals tag.Id into tagRows
                      from tag in tagRows.DefaultIfEmpty()
                      orderby segment.Name
                      select new CustomerSegmentDto(segment.Id, segment.Name, segment.RequiredTagId, tag == null ? null : tag.Name,
                          segment.MinimumLifetimeValue, segment.MaximumLifetimeValue, segment.MinimumSalesCount,
                          segment.PurchasedWithinDays, segment.ActiveCustomersOnly, segment.Revision, segment.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerSegmentMembersDto> GetMembersAsync(Guid segmentId, int limit = 200, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var segment = await LoadSegmentDtoAsync(db, segmentId, cancellationToken)
            ?? throw new DomainException("El segmento seleccionado no existe en esta empresa.");
        var entity = await db.CustomerSegments.AsNoTracking().SingleAsync(x => x.Id == segmentId, cancellationToken);
        var query = BuildMembersQuery(db, entity);
        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query.OrderByDescending(x => x.LifetimeValue).ThenBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(x => new CustomerDto(x.Id, x.FirstName + " " + x.LastName, x.Email, x.Phone, x.Company,
                x.LifetimeValue, x.Revision, x.FirstName, x.LastName, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new CustomerSegmentMembersDto(segment, totalCount, totalCount > customers.Count, customers);
    }

    public async Task<CustomerSegmentDto> CreateAsync(CreateCustomerSegmentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await EnsureTagBelongsToTenantAsync(db, request.RequiredTagId, cancellationToken);
        var name = request.Name?.Trim() ?? string.Empty;
        var normalized = name.ToUpperInvariant();
        if (await db.CustomerSegments.AnyAsync(x => x.NormalizedName == normalized, cancellationToken))
            throw new DomainException("Ya existe un segmento con ese nombre en esta empresa.");
        var segment = new CustomerSegment(tenantId, name, request.RequiredTagId, request.MinimumLifetimeValue,
            request.MaximumLifetimeValue, request.MinimumSalesCount, request.PurchasedWithinDays, request.ActiveCustomersOnly);
        db.CustomerSegments.Add(segment);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un segmento con ese nombre en esta empresa."); }
        return await LoadSegmentDtoAsync(db, segment.Id, cancellationToken) ?? throw new InvalidOperationException("No se pudo volver a leer el segmento creado.");
    }

    public async Task<CustomerSegmentDto> UpdateAsync(Guid segmentId, UpdateCustomerSegmentRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var segment = await db.CustomerSegments.SingleOrDefaultAsync(x => x.Id == segmentId, cancellationToken)
            ?? throw new DomainException("El segmento seleccionado no existe.");
        if (segment.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El segmento cambió en otra sesión. Recarga la lista antes de guardar.");
        await EnsureTagBelongsToTenantAsync(db, request.RequiredTagId, cancellationToken);
        var normalized = request.Name?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.CustomerSegments.AnyAsync(x => x.Id != segmentId && x.NormalizedName == normalized, cancellationToken))
            throw new DomainException("Ya existe un segmento con ese nombre en esta empresa.");
        segment.Update(request.Name ?? string.Empty, request.RequiredTagId, request.MinimumLifetimeValue, request.MaximumLifetimeValue,
            request.MinimumSalesCount, request.PurchasedWithinDays, request.ActiveCustomersOnly);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El segmento cambió en otra sesión. Recarga la lista antes de guardar."); }
        catch (DbUpdateException) { throw new DomainException("Ya existe un segmento con ese nombre en esta empresa."); }
        return await LoadSegmentDtoAsync(db, segment.Id, cancellationToken) ?? throw new InvalidOperationException("No se pudo volver a leer el segmento actualizado.");
    }

    public async Task DeleteAsync(Guid segmentId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var segment = await db.CustomerSegments.SingleOrDefaultAsync(x => x.Id == segmentId, cancellationToken)
            ?? throw new DomainException("El segmento seleccionado no existe.");
        if (segment.Revision != expectedRevision)
            throw new ConcurrencyConflictException("El segmento cambió en otra sesión. Recarga la lista antes de eliminarlo.");
        db.CustomerSegments.Remove(segment);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El segmento cambió en otra sesión. Recarga la lista antes de eliminarlo."); }
    }

    private IQueryable<Customer> BuildMembersQuery(StoreDbContext db, CustomerSegment segment)
    {
        var query = db.Customers.AsNoTracking();
        if (segment.ActiveCustomersOnly) query = query.Where(x => x.IsActive);
        if (segment.MinimumLifetimeValue is { } minimum) query = query.Where(x => x.LifetimeValue >= minimum);
        if (segment.MaximumLifetimeValue is { } maximum) query = query.Where(x => x.LifetimeValue <= maximum);
        if (segment.RequiredTagId is { } tagId)
            query = query.Where(customer => db.CustomerTagAssignments.Any(assignment => assignment.CustomerId == customer.Id && assignment.CustomerTagId == tagId));
        if (segment.MinimumSalesCount is { } salesCount)
            query = query.Where(customer => db.Sales.Count(sale => sale.CustomerId == customer.Id && sale.Status == SaleStatus.Completed) >= salesCount);
        if (segment.PurchasedWithinDays is { } days)
        {
            var sinceUtc = clock.UtcNow.AddDays(-days);
            query = query.Where(customer => db.Sales.Any(sale => sale.CustomerId == customer.Id && sale.Status == SaleStatus.Completed && sale.CreatedAtUtc >= sinceUtc));
        }
        return query;
    }

    private async Task EnsureTagBelongsToTenantAsync(StoreDbContext db, Guid? tagId, CancellationToken ct)
    {
        if (tagId is { } id && !await db.CustomerTags.AnyAsync(x => x.Id == id, ct))
            throw new DomainException("La etiqueta seleccionada no pertenece a esta empresa.");
    }

    private async Task<CustomerSegmentDto?> LoadSegmentDtoAsync(StoreDbContext db, Guid id, CancellationToken ct) =>
        await (from segment in db.CustomerSegments.AsNoTracking()
               join tag in db.CustomerTags.AsNoTracking() on segment.RequiredTagId equals tag.Id into tagRows
               from tag in tagRows.DefaultIfEmpty()
               where segment.Id == id
               select new CustomerSegmentDto(segment.Id, segment.Name, segment.RequiredTagId, tag == null ? null : tag.Name,
                   segment.MinimumLifetimeValue, segment.MaximumLifetimeValue, segment.MinimumSalesCount,
                   segment.PurchasedWithinDays, segment.ActiveCustomersOnly, segment.Revision, segment.CreatedAtUtc))
            .SingleOrDefaultAsync(ct);

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken ct)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, ct);
        var db = await contextFactory.CreateDbContextAsync(ct);
        db.CurrentTenantId = tenantId;
        return db;
    }
}
