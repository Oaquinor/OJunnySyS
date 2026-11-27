using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Infrastructure.Persistence;
using OJunnySyS.Infrastructure.Identity;

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
        services.AddScoped<IPosRegisterRepository, EfPosRegisterRepository>();
        services.AddScoped<IWarehouseRepository, EfWarehouseRepository>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IRegistrationEmailSender, SmtpRegistrationEmailSender>();
        services.AddScoped<ITenantAccessGuard, TenantAccessGuard>();
        services.AddSingleton<IBusinessClock>(_ => new SystemBusinessClock(businessTimeZone));
        return services;
    }
}
