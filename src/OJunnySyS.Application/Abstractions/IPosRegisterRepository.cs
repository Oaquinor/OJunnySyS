using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Application.Abstractions;

public interface IPosRegisterRepository
{
    Task<IReadOnlyList<PosRegister>> GetRegistersAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<PosRegister> CreateRegisterAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<PosRegister> UpdateRegisterAsync(Guid id, string name, long expectedRevision, CancellationToken cancellationToken = default);
    Task<PosRegister> SetRegisterActiveAsync(Guid id, bool isActive, long expectedRevision, CancellationToken cancellationToken = default);
}
