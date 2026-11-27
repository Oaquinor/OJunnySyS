using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface IPosPaymentMethodRepository
{
    Task<IReadOnlyList<TenantPaymentMethodDto>> GetAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<TenantPaymentMethodDto> CreateAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<TenantPaymentMethodDto> UpdateAsync(Guid id, UpdatePaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<TenantPaymentMethodDto> SetActiveAsync(Guid id, SetPaymentMethodActiveRequest request, CancellationToken cancellationToken = default);
}
