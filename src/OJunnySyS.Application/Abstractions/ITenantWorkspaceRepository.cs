using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ITenantWorkspaceRepository
{
    Task<TenantWorkspaceDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
