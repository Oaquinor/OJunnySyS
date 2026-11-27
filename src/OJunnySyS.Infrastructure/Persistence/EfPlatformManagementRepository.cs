using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfPlatformManagementRepository(
    StoreDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IPlatformManagementRepository
{
    public async Task<IReadOnlyList<(Tenant Tenant, int UserCount)>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await db.Tenants.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var counts = await db.Users.Where(x => x.TenantId != null).GroupBy(x => x.TenantId!.Value)
            .Select(x => new { TenantId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);
        return tenants.Select(x => (x, counts.GetValueOrDefault(x.Id))).ToArray();
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant, string administratorName, string administratorEmail, string initialPassword, CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.AnyAsync(x => x.Slug == tenant.Slug, cancellationToken)) throw new DomainException("Ese identificador de empresa ya está ocupado.");
        if (await userManager.FindByEmailAsync(administratorEmail.Trim()) is not null) throw new DomainException("Ese correo ya tiene una cuenta en la plataforma.");
        if (!await roleManager.RoleExistsAsync(AppRoles.Administrator)) throw new InvalidOperationException("El rol de administrador de empresa no está inicializado.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = administratorEmail.Trim(),
            Email = administratorEmail.Trim(),
            DisplayName = administratorName.Trim(),
            TenantId = tenant.Id,
            EmailConfirmed = false,
            MustChangePassword = true,
            LockoutEnabled = true
        };
        var createResult = await userManager.CreateAsync(user, initialPassword);
        if (!createResult.Succeeded) throw new DomainException(string.Join(" ", createResult.Errors.Select(x => x.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
        if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(x => x.Description)));
        await transaction.CommitAsync(cancellationToken);
        return tenant;
    }

    public async Task<Tenant> UpdateTenantStatusAsync(Guid tenantId, TenantStatus status, string? planCode = null, CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró la empresa seleccionada.");
        if (status == TenantStatus.Suspended) tenant.Suspend();
        else if (status == TenantStatus.Active) tenant.Activate(planCode ?? tenant.PlanCode);
        else throw new DomainException("Solo se puede suspender o activar desde este panel.");
        await db.SaveChangesAsync(cancellationToken);
        return tenant;
    }
}
