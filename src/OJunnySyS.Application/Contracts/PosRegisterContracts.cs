using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Application.Contracts;

public sealed record PosRegisterDto(Guid Id, string Code, string Name, bool IsActive, long Revision, DateTimeOffset CreatedAtUtc);
public sealed record CreatePosRegisterRequest(string Code, string Name);
public sealed record UpdatePosRegisterRequest(string Name, long ExpectedRevision);
public sealed record SetPosRegisterActiveRequest(bool IsActive, long ExpectedRevision);

public static class PosRegisterMappings
{
    public static PosRegisterDto ToDto(this PosRegister register) =>
        new(register.Id, register.Code, register.Name, register.IsActive, register.Revision, register.CreatedAtUtc);
}
