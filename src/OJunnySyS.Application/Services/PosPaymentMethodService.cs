using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class PosPaymentMethodService(IPosPaymentMethodRepository repository)
{
    public Task<IReadOnlyList<TenantPaymentMethodDto>> GetAsync(bool includeInactive = false, CancellationToken ct = default) =>
        repository.GetAsync(includeInactive, ct);
    public Task<TenantPaymentMethodDto> CreateAsync(CreatePaymentMethodRequest request, CancellationToken ct = default) => repository.CreateAsync(request, ct);
    public Task<TenantPaymentMethodDto> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, CancellationToken ct = default) => repository.UpdateAsync(id, request, ct);
    public Task<TenantPaymentMethodDto> SetActiveAsync(Guid id, SetPaymentMethodActiveRequest request, CancellationToken ct = default) => repository.SetActiveAsync(id, request, ct);
}
