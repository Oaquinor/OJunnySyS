using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class TenantUserService(ITenantUserRepository repository)
{
    public Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(CancellationToken ct = default) => repository.GetUsersAsync(ct);
    public Task<IReadOnlyList<TenantMemberDto>> GetActiveMembersAsync(CancellationToken ct = default) => repository.GetActiveMembersAsync(ct);
    public Task<IReadOnlyList<TenantMemberDto>> GetActiveSalespeopleAsync(CancellationToken ct = default) => repository.GetActiveSalespeopleAsync(ct);
    public Task<ProvisionedTenantUserDto> CreateAsync(CreateTenantUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 160)
            throw new ArgumentException("El nombre es obligatorio y admite hasta 160 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 254 || !request.Email.Contains('@'))
            throw new ArgumentException("Escribe un correo electrónico válido.");
        if (!TenantUserRoles.Assignable.Contains(request.Role, StringComparer.Ordinal))
            throw new ArgumentException("El rol seleccionado no se puede asignar.");
        return repository.CreateAsync(request with { DisplayName = request.DisplayName.Trim(), Email = request.Email.Trim(), Role = request.Role.Trim() }, ct);
    }
    public Task<TenantUserDto> SetActiveAsync(Guid userId, Guid actorId, bool isActive, CancellationToken ct = default) => repository.SetActiveAsync(userId, actorId, isActive, ct);
}
