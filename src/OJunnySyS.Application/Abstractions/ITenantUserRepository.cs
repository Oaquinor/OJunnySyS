using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ITenantUserRepository
{
    Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMemberDto>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMemberDto>> GetActiveSalespeopleAsync(CancellationToken cancellationToken = default);
    Task<ProvisionedTenantUserDto> CreateAsync(CreateTenantUserRequest request, CancellationToken cancellationToken = default);
    Task<TenantUserDto> SetActiveAsync(Guid userId, Guid actorId, bool isActive, CancellationToken cancellationToken = default);
}
