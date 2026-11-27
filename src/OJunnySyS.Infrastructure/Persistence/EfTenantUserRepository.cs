using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfTenantUserRepository(
    StoreDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : ITenantUserRepository
{
    public async Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var tenantUsers = await db.Users.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);
        var result = new List<TenantUserDto>(tenantUsers.Count);
        foreach (var user in tenantUsers)
        {
            var assigned = await users.GetRolesAsync(user);
            result.Add(ToDto(user, assigned.FirstOrDefault(TenantUserRoles.Assignable.Contains) ?? "Sin rol"));
        }
        return result;
    }

    public async Task<IReadOnlyList<TenantMemberDto>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var businessRoles = new[] { AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson, AppRoles.PurchasingAgent, AppRoles.Cashier };
        return await (from user in db.Users.AsNoTracking()
                      join userRole in db.UserRoles on user.Id equals userRole.UserId
                      join role in db.Roles on userRole.RoleId equals role.Id
                      where user.TenantId == tenantId && (user.LockoutEnd == null || user.LockoutEnd <= now) &&
                            role.Name != null && businessRoles.Contains(role.Name)
                      orderby user.DisplayName
                      select new TenantMemberDto(user.Id, user.DisplayName)).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMemberDto>> GetActiveSalespeopleAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var sellerRoles = new[] { AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson };
        return await (from user in db.Users.AsNoTracking()
                      join userRole in db.UserRoles on user.Id equals userRole.UserId
                      join role in db.Roles on userRole.RoleId equals role.Id
                      where user.TenantId == tenantId && (user.LockoutEnd == null || user.LockoutEnd <= now) &&
                            role.Name != null && sellerRoles.Contains(role.Name)
                      orderby user.DisplayName
                      select new TenantMemberDto(user.Id, user.DisplayName)).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<ProvisionedTenantUserDto> CreateAsync(CreateTenantUserRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        if (await users.FindByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException("Ya existe una cuenta con ese correo electrónico.");

        var role = await roles.FindByNameAsync(request.Role) ?? throw new InvalidOperationException("El rol solicitado no está configurado.");
        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = request.Email, Email = request.Email,
            DisplayName = request.DisplayName, TenantId = tenantId,
            EmailConfirmed = false, MustChangePassword = true, LockoutEnabled = true
        };
        var create = await users.CreateAsync(user, temporaryPassword);
        if (!create.Succeeded) throw new InvalidOperationException(string.Join(" ", create.Errors.Select(x => x.Description)));
        var assign = await users.AddToRoleAsync(user, role.Name!);
        if (!assign.Succeeded)
        {
            await users.DeleteAsync(user);
            throw new InvalidOperationException("No se pudo asignar el rol seleccionado.");
        }
        return new ProvisionedTenantUserDto(ToDto(user, role.Name!), temporaryPassword);
    }

    public async Task<TenantUserDto> SetActiveAsync(Guid userId, Guid actorId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && userId == actorId) throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("La cuenta no existe en esta empresa.");
        var assigned = await users.GetRolesAsync(user);
        var role = assigned.FirstOrDefault(TenantUserRoles.Assignable.Contains) ?? "Sin rol";
        if (!isActive && role == TenantUserRoles.Administrator)
        {
            var administratorRole = await roles.FindByNameAsync(TenantUserRoles.Administrator)
                ?? throw new InvalidOperationException("El rol de administrador no está configurado.");
            var activeAdmins = await (from userRole in db.UserRoles
                                      join candidate in db.Users on userRole.UserId equals candidate.Id
                                      where userRole.RoleId == administratorRole.Id && candidate.TenantId == tenantId
                                      select candidate).CountAsync(x => x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow, cancellationToken);
            if (activeAdmins <= 1) throw new InvalidOperationException("La empresa debe conservar al menos un administrador activo.");
        }
        user.LockoutEnabled = true;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) throw new InvalidOperationException(string.Join(" ", update.Errors.Select(x => x.Description)));
        return ToDto(user, role);
    }

    private static TenantUserDto ToDto(ApplicationUser user, string role) =>
        new(user.Id, user.DisplayName, user.Email ?? string.Empty, role, user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow, user.MustChangePassword, user.LockoutEnd);
}
