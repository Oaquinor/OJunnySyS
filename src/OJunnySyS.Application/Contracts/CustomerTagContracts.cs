using OJunnySyS.Domain.Customers;

namespace OJunnySyS.Application.Contracts;

public sealed record CustomerTagDto(Guid Id, string Name, CustomerTagColor Color, int CustomerCount = 0);
public sealed record CustomerTagAssignmentDto(Guid Id, string Name, CustomerTagColor Color, DateTimeOffset AssignedAtUtc, Guid AssignedByUserId);
public sealed record AddCustomerTagRequest(string Name, CustomerTagColor Color);
