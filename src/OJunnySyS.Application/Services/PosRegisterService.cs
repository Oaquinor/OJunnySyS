using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public sealed class PosRegisterService(IPosRegisterRepository repository)
{
    public async Task<IReadOnlyList<PosRegisterDto>> GetRegistersAsync(bool includeInactive = true, CancellationToken ct = default) =>
        (await repository.GetRegistersAsync(includeInactive, ct)).Select(x => x.ToDto()).ToArray();

    public async Task<PosRegisterDto> CreateAsync(CreatePosRegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return (await repository.CreateRegisterAsync(request.Code, request.Name, request.WarehouseId, ct)).ToDto();
    }

    public async Task<PosRegisterDto> UpdateAsync(Guid id, UpdatePosRegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id == Guid.Empty) throw new DomainException("Selecciona un terminal válido.");
        return (await repository.UpdateRegisterAsync(id, request.Name, request.WarehouseId, request.ExpectedRevision, ct)).ToDto();
    }

    public async Task<PosRegisterDto> SetActiveAsync(Guid id, SetPosRegisterActiveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id == Guid.Empty) throw new DomainException("Selecciona un terminal válido.");
        return (await repository.SetRegisterActiveAsync(id, request.IsActive, request.ExpectedRevision, ct)).ToDto();
    }
}
