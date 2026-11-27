using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ITenantSettingsRepository
{
    Task<TenantBusinessSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<TenantBusinessSettingsDto> UpdateAsync(UpdateTenantBusinessSettingsRequest request, CancellationToken cancellationToken = default);
}
