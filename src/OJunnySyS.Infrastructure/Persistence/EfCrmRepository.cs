using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Crm;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfCrmRepository(IDbContextFactory<StoreDbContext> contextFactory, ITenantContext tenantContext, ITenantAccessGuard tenantAccess, IBusinessClock clock) : ICrmRepository
{
    public async Task<IReadOnlyList<OpportunityDto>> GetOpportunitiesAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var query = from opportunity in db.Opportunities.AsNoTracking()
                      join customer in db.Customers.AsNoTracking() on opportunity.CustomerId equals customer.Id
                      select new { opportunity, customer };
        if (customerId is { } selectedCustomerId) query = query.Where(x => x.opportunity.CustomerId == selectedCustomerId);
        var result = await query.OrderBy(x => x.opportunity.Stage).ThenBy(x => x.opportunity.ExpectedCloseDate).ThenByDescending(x => x.opportunity.CreatedAtUtc)
                      .Select(x => new OpportunityDto(x.opportunity.Id, x.opportunity.CustomerId, x.customer.FirstName + " " + x.customer.LastName,
                          x.opportunity.Title, x.opportunity.ExpectedValue, x.opportunity.ExpectedCloseDate, x.opportunity.Description,
                          x.opportunity.Stage, x.opportunity.CreatedAtUtc, x.opportunity.AssignedToUserId)).Take(300).ToListAsync(cancellationToken);
        return await AddAssigneeNamesAsync(db, result, tenantContext.GetRequiredTenantId(), cancellationToken);
    }

    public async Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El cliente seleccionado no existe o está inactivo.");
        var assigneeName = await ValidateAssigneeAsync(db, tenantId, request.AssignedToUserId, cancellationToken);
        var opportunity = new Opportunity(tenantId, customer.Id, request.Title, request.ExpectedValue, request.ExpectedCloseDate, request.Description, request.AssignedToUserId);
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(opportunity, customer.FullName, assigneeName);
    }

    public async Task<OpportunityDto> MoveOpportunityAsync(Guid id, OpportunityStage stage, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var opportunity = await db.Opportunities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("La oportunidad no existe.");
        var customer = await db.Customers.SingleAsync(x => x.Id == opportunity.CustomerId, cancellationToken);
        opportunity.MoveTo(stage, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(opportunity, customer.FullName, await GetAssigneeNameAsync(db, opportunity.AssignedToUserId, cancellationToken));
    }

    public async Task<IReadOnlyList<CrmActivityDto>> GetActivitiesAsync(Guid? customerId = null, bool openTasksOnly = false, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var query = from activity in db.CrmActivities.AsNoTracking()
                    join customer in db.Customers.AsNoTracking() on activity.CustomerId equals customer.Id
                    select new { activity, customer };
        if (customerId is { } id) query = query.Where(x => x.activity.CustomerId == id);
        if (openTasksOnly) query = query.Where(x => x.activity.Kind == CrmActivityKind.Task && x.activity.CompletedAtUtc == null);
        var result = await query.OrderBy(x => x.activity.DueAtUtc == null).ThenBy(x => x.activity.DueAtUtc ?? x.activity.OccurredAtUtc)
            .ThenByDescending(x => x.activity.OccurredAtUtc).Take(300)
            .Select(x => new CrmActivityDto(x.activity.Id, x.activity.CustomerId, x.customer.FirstName + " " + x.customer.LastName,
                x.activity.OpportunityId, x.activity.CreatedByUserId, x.activity.Kind, x.activity.Subject, x.activity.Details,
                x.activity.OccurredAtUtc, x.activity.DueAtUtc, x.activity.CompletedAtUtc, x.activity.AssignedToUserId)).ToListAsync(cancellationToken);
        return await AddAssigneeNamesAsync(db, result, tenantContext.GetRequiredTenantId(), cancellationToken);
    }

    public async Task<CrmActivityDto> CreateActivityAsync(CreateCrmActivityRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Kind)) throw new DomainException("El tipo de actividad no es válido.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El cliente seleccionado no existe o está inactivo.");
        if (request.OpportunityId is { } opportunityId && !await db.Opportunities.AnyAsync(x => x.Id == opportunityId && x.CustomerId == customer.Id, cancellationToken))
            throw new DomainException("La oportunidad no pertenece al cliente seleccionado.");
        var assigneeName = await ValidateAssigneeAsync(db, tenantId, request.AssignedToUserId, cancellationToken);
        var activity = new CrmActivity(tenantId, customer.Id, request.OpportunityId, actorId, request.Kind,
            request.Subject, request.Details, clock.UtcNow, request.DueAtUtc, request.AssignedToUserId);
        db.CrmActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(activity, customer.FullName, assigneeName);
    }

    public async Task<CrmActivityDto> CompleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var activity = await db.CrmActivities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("La tarea no existe.");
        var customer = await db.Customers.SingleAsync(x => x.Id == activity.CustomerId, cancellationToken);
        activity.Complete(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(activity, customer.FullName, await GetAssigneeNameAsync(db, activity.AssignedToUserId, cancellationToken));
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }

    private async Task<string?> ValidateAssigneeAsync(StoreDbContext db, Guid tenantId, Guid? assignedToUserId, CancellationToken ct)
    {
        if (assignedToUserId is null) return null;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assignedToUserId && x.TenantId == tenantId, ct)
            ?? throw new DomainException("El responsable debe ser un usuario de esta empresa.");
        if (user.LockoutEnd is { } until && until > clock.UtcNow) throw new DomainException("El responsable seleccionado está desactivado o bloqueado.");
        var accessRoles = new[] { AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson, AppRoles.PurchasingAgent, AppRoles.Cashier };
        var hasBusinessRole = await (from userRole in db.UserRoles
                                     join role in db.Roles on userRole.RoleId equals role.Id
                                     where userRole.UserId == user.Id && role.Name != null && accessRoles.Contains(role.Name)
                                     select userRole.UserId).AnyAsync(ct);
        if (!hasBusinessRole) throw new DomainException("El responsable no tiene un rol de acceso al negocio.");
        return user.DisplayName;
    }

    private async Task<string?> GetAssigneeNameAsync(StoreDbContext db, Guid? id, CancellationToken ct) => id is null ? null :
        await db.Users.AsNoTracking().Where(x => x.Id == id && x.TenantId == tenantContext.GetRequiredTenantId()).Select(x => x.DisplayName).SingleOrDefaultAsync(ct);

    private static async Task<IReadOnlyList<TDto>> AddAssigneeNamesAsync<TDto>(StoreDbContext db, IReadOnlyList<TDto> items, Guid tenantId, CancellationToken ct)
    {
        var ids = items.Select(x => x switch { OpportunityDto o => o.AssignedToUserId, CrmActivityDto a => a.AssignedToUserId, _ => null })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var names = ids.Length == 0 ? new Dictionary<Guid, string>() : await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id) && x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
        return items.Select(x => x switch
        {
            OpportunityDto o => (TDto)(object)(o with { AssignedToName = o.AssignedToUserId is { } id && names.TryGetValue(id, out var name) ? name : null }),
            CrmActivityDto a => (TDto)(object)(a with { AssignedToName = a.AssignedToUserId is { } id && names.TryGetValue(id, out var name) ? name : null }),
            _ => x
        }).ToArray();
    }

    private static OpportunityDto Map(Opportunity opportunity, string customerName, string? assignedToName = null) => new(opportunity.Id, opportunity.CustomerId,
        customerName, opportunity.Title, opportunity.ExpectedValue, opportunity.ExpectedCloseDate, opportunity.Description,
        opportunity.Stage, opportunity.CreatedAtUtc, opportunity.AssignedToUserId, assignedToName);

    private static CrmActivityDto Map(CrmActivity activity, string customerName, string? assignedToName = null) => new(activity.Id, activity.CustomerId,
        customerName, activity.OpportunityId, activity.CreatedByUserId, activity.Kind, activity.Subject, activity.Details,
        activity.OccurredAtUtc, activity.DueAtUtc, activity.CompletedAtUtc, activity.AssignedToUserId, assignedToName);
}
