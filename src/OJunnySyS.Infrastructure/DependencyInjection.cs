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
        services.AddScoped<IParkedSaleRepository, EfParkedSaleRepository>();
        services.AddScoped<ISalesQuoteRepository, EfSalesQuoteRepository>();
        services.AddScoped<ICrmRepository, EfCrmRepository>();
        services.AddScoped<IPlatformManagementRepository, EfPlatformManagementRepository>();
        services.AddScoped<ITenantSettingsRepository, EfTenantSettingsRepository>();
        services.AddScoped<ITenantWorkspaceRepository, EfTenantWorkspaceRepository>();
        services.AddScoped<ITenantUserRepository, EfTenantUserRepository>();
        services.AddScoped<ICustomerTagRepository, EfCustomerTagRepository>();
        services.AddScoped<ICustomerSegmentRepository, EfCustomerSegmentRepository>();
        services.AddScoped<IPosRegisterRepository, EfPosRegisterRepository>();
        services.AddScoped<IPosOfflineDeviceRepository, EfPosOfflineDeviceRepository>();
        services.AddScoped<IWarehouseRepository, EfWarehouseRepository>();
        services.AddScoped<IPosPaymentMethodRepository, EfPosPaymentMethodRepository>();
        services.AddScoped<ITaxProfileRepository, EfTaxProfileRepository>();
        services.AddScoped<IAccountingRepository, EfAccountingRepository>();
        services.AddScoped<IBankingRepository, EfBankingRepository>();
        services.AddScoped<IReportTemplateRepository, EfReportTemplateRepository>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
        services.AddScoped<ITenantAccessGuard, TenantAccessGuard>();
        services.AddSingleton<IBusinessClock>(_ => new SystemBusinessClock(businessTimeZone));
        return services;
    }
}
