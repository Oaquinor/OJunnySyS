using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Infrastructure.Persistence;

namespace OJunnySyS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string businessTimeZone)
    {
        services.AddDbContextFactory<StoreDbContext>(options => options.UseMySQL(connectionString));
        services.AddScoped<IStoreRepository, EfStoreRepository>();
        services.AddScoped<ICrmRepository, EfCrmRepository>();
        services.AddScoped<IPlatformManagementRepository, EfPlatformManagementRepository>();
        services.AddScoped<ITenantSettingsRepository, EfTenantSettingsRepository>();
        services.AddScoped<ITenantUserRepository, EfTenantUserRepository>();
        services.AddScoped<ICustomerTagRepository, EfCustomerTagRepository>();
        services.AddScoped<ICustomerSegmentRepository, EfCustomerSegmentRepository>();
        services.AddScoped<ITenantAccessGuard, TenantAccessGuard>();
        services.AddSingleton<IBusinessClock>(_ => new SystemBusinessClock(businessTimeZone));
        return services;
    }
}
