using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class TenantSettingsService(ITenantSettingsRepository repository)
{
    public Task<TenantBusinessSettingsDto> GetAsync(CancellationToken cancellationToken = default) => repository.GetAsync(cancellationToken);

    public Task<TenantBusinessSettingsDto> UpdateAsync(UpdateTenantBusinessSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return repository.UpdateAsync(request with
        {
            TimeZoneId = request.TimeZoneId?.Trim() ?? string.Empty,
            CurrencyCode = request.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty
        }, cancellationToken);
    }
}
