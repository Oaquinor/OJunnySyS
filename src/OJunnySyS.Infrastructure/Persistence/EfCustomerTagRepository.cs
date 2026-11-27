using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfCustomerTagRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : ICustomerTagRepository
{
    public async Task<IReadOnlyList<CustomerTagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var counts = await db.CustomerTagAssignments.AsNoTracking().GroupBy(x => x.CustomerTagId)
            .Select(x => new { TagId = x.Key, Count = x.Select(a => a.CustomerId).Distinct().Count() })
            .ToDictionaryAsync(x => x.TagId, x => x.Count, cancellationToken);
        var tags = await db.CustomerTags.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return tags.Select(x => new CustomerTagDto(x.Id, x.Name, x.Color, counts.GetValueOrDefault(x.Id))).ToArray();
    }

    public async Task<IReadOnlyList<CustomerTagAssignmentDto>> GetCustomerTagsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, cancellationToken)) return [];
        return await (from assignment in db.CustomerTagAssignments.AsNoTracking()
                      join tag in db.CustomerTags.AsNoTracking() on assignment.CustomerTagId equals tag.Id
                      where assignment.CustomerId == customerId
                      orderby tag.Name
                      select new CustomerTagAssignmentDto(tag.Id, tag.Name, tag.Color, assignment.CreatedAtUtc, assignment.AssignedByUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerTagAssignmentDto> AddToCustomerAsync(Guid customerId, AddCustomerTagRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El cliente no existe o está inactivo.");
        var tagName = request.Name.Trim();
        var normalizedName = tagName.ToUpperInvariant();
        var tag = await db.CustomerTags.SingleOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
        if (tag is null)
        {
            tag = new CustomerTag(tenantId, tagName, request.Color);
            db.CustomerTags.Add(tag);
        }
        var existing = await db.CustomerTagAssignments.SingleOrDefaultAsync(x => x.CustomerId == customer.Id && x.CustomerTagId == tag.Id, cancellationToken);
        if (existing is not null) return Map(existing, tag);

        var assignment = new CustomerTagAssignment(tenantId, customer.Id, tag.Id, actorId);
        db.CustomerTagAssignments.Add(assignment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Map(assignment, tag);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var savedTag = await db.CustomerTags.SingleOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
            if (savedTag is not null)
            {
                var savedAssignment = await db.CustomerTagAssignments.SingleOrDefaultAsync(
                    x => x.CustomerId == customer.Id && x.CustomerTagId == savedTag.Id, cancellationToken);
                if (savedAssignment is not null) return Map(savedAssignment, savedTag);

                var retry = new CustomerTagAssignment(tenantId, customer.Id, savedTag.Id, actorId);
                db.CustomerTagAssignments.Add(retry);
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                    return Map(retry, savedTag);
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    var concurrentAssignment = await db.CustomerTagAssignments.SingleOrDefaultAsync(
                        x => x.CustomerId == customer.Id && x.CustomerTagId == savedTag.Id, cancellationToken);
                    if (concurrentAssignment is not null) return Map(concurrentAssignment, savedTag);
                }
            }
            throw new DomainException("No se pudo guardar la etiqueta. Actualiza la ficha e inténtalo de nuevo.");
        }
    }

    public async Task RemoveFromCustomerAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var assignment = await db.CustomerTagAssignments.SingleOrDefaultAsync(
            x => x.CustomerId == customerId && x.CustomerTagId == tagId, cancellationToken);
        if (assignment is null) return;
        db.CustomerTagAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }

    private static CustomerTagAssignmentDto Map(CustomerTagAssignment assignment, CustomerTag tag) =>
        new(tag.Id, tag.Name, tag.Color, assignment.CreatedAtUtc, assignment.AssignedByUserId);
}
