using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public sealed class WarehouseService(IWarehouseRepository repository)
{
    public async Task<IReadOnlyList<WarehouseDto>> GetAsync(bool includeInactive = true, CancellationToken ct = default) =>
        (await repository.GetWarehousesAsync(includeInactive, ct)).Select(x => x.ToDto()).ToArray();

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return (await repository.CreateWarehouseAsync(request.Code, request.Name, ct)).ToDto();
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id == Guid.Empty) throw new DomainException("Selecciona un almacén válido.");
        return (await repository.UpdateWarehouseAsync(id, request.Name, request.ExpectedRevision, ct)).ToDto();
    }

    public async Task<WarehouseDto> SetActiveAsync(Guid id, SetWarehouseActiveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id == Guid.Empty) throw new DomainException("Selecciona un almacén válido.");
        return (await repository.SetWarehouseActiveAsync(id, request.IsActive, request.ExpectedRevision, ct)).ToDto();
    }
}
