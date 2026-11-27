using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;

namespace OJunnySyS.Application.Services;

public sealed class TaxProfileService(ITaxProfileRepository repository)
{
    public async Task<IReadOnlyList<TaxProfileDto>> GetAsync(bool includeInactive = false, CancellationToken ct = default) =>
        (await repository.GetAsync(includeInactive, ct)).Select(Map).ToArray();

    public async Task<TaxProfileDto> CreateAsync(CreateTaxProfileRequest request, CancellationToken ct = default) =>
        Map(await repository.CreateAsync(request, ct));

    public async Task<TaxProfileDto> UpdateAsync(Guid id, UpdateTaxProfileRequest request, CancellationToken ct = default) =>
        Map(await repository.UpdateAsync(id, request, ct));

    public async Task<TaxProfileDto> SetActiveAsync(Guid id, SetTaxProfileActiveRequest request, CancellationToken ct = default) =>
        Map(await repository.SetActiveAsync(id, request, ct));

    private static TaxProfileDto Map(TaxProfile profile) => new(profile.Id, profile.Code, profile.Name, profile.Rate, profile.IsActive, profile.Revision);
}
