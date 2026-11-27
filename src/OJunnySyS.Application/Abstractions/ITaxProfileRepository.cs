using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;

namespace OJunnySyS.Application.Abstractions;

public interface ITaxProfileRepository
{
    Task<IReadOnlyList<TaxProfile>> GetAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<TaxProfile> CreateAsync(CreateTaxProfileRequest request, CancellationToken cancellationToken = default);
    Task<TaxProfile> UpdateAsync(Guid id, UpdateTaxProfileRequest request, CancellationToken cancellationToken = default);
    Task<TaxProfile> SetActiveAsync(Guid id, SetTaxProfileActiveRequest request, CancellationToken cancellationToken = default);
}
