using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OJunnySyS.Infrastructure.Persistence;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Infrastructure.Identity;

public static class IdentityInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityInitializer");
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<StoreDbContext>>();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == TenantDefaults.LocalDemoTenantId, ct);
        if (tenant is null)
        {
            tenant = new Tenant("Ojunny Demo", TenantDefaults.LocalDemoTenantSlug,
                configuration["BootstrapAdmin:Email"] ?? "admin@ojunny.local", "America/Santo_Domingo");
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
        }
        db.CurrentTenantId = tenant.Id;
        if (!await db.PosRegisters.AnyAsync(x => x.Code == "MAIN", ct))
        {
            db.PosRegisters.Add(new PosRegister(tenant.Id, "MAIN", "Caja principal"));
            await db.SaveChangesAsync(ct);
        }

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded) throw new InvalidOperationException($"No se pudo crear el rol {role}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }

        var email = configuration["BootstrapAdmin:Email"]?.Trim();
        var password = configuration["BootstrapAdmin:Password"];
        var displayName = configuration["BootstrapAdmin:DisplayName"]?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se configuró BootstrapAdmin. Los roles existen, pero no se creó una cuenta inicial.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName ?? email, TenantId = tenant.Id };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException($"No se pudo crear el administrador inicial: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }

        if (user.TenantId is null)
        {
            user.TenantId = tenant.Id;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) throw new InvalidOperationException($"No se pudo asociar el administrador con la empresa de demostración: {string.Join("; ", updateResult.Errors.Select(x => x.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Administrator))
        {
            var result = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
            if (!result.Succeeded) throw new InvalidOperationException($"No se pudo asignar el rol administrador: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
        if (!await userManager.IsInRoleAsync(user, AppRoles.PlatformAdministrator))
        {
            var result = await userManager.AddToRoleAsync(user, AppRoles.PlatformAdministrator);
            if (!result.Succeeded) throw new InvalidOperationException($"No se pudo asignar el rol administrador de plataforma: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }
}
