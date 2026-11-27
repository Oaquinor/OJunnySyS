using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class TenantWorkspaceService(ITenantWorkspaceRepository repository)
{
    public Task<TenantWorkspaceDto> GetOverviewAsync(CancellationToken cancellationToken = default) =>
        repository.GetOverviewAsync(cancellationToken);
}
