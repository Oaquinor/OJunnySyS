using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Application.Contracts;

public sealed record PosRegisterDto(Guid Id, string Code, string Name, Guid WarehouseId, bool IsActive, long Revision, DateTimeOffset CreatedAtUtc);
public sealed record CreatePosRegisterRequest(string Code, string Name, Guid WarehouseId);
public sealed record UpdatePosRegisterRequest(string Name, Guid WarehouseId, long ExpectedRevision);
public sealed record SetPosRegisterActiveRequest(bool IsActive, long ExpectedRevision);

public static class PosRegisterMappings
{
    public static PosRegisterDto ToDto(this PosRegister register) =>
        new(register.Id, register.Code, register.Name, register.WarehouseId, register.IsActive, register.Revision, register.CreatedAtUtc);
}
