namespace OJunnySyS.Application.Contracts;

public sealed record CreateCustomerSegmentRequest(string Name, Guid? RequiredTagId, decimal? MinimumLifetimeValue,
    decimal? MaximumLifetimeValue, int? MinimumSalesCount, int? PurchasedWithinDays, bool ActiveCustomersOnly = true);
public sealed record UpdateCustomerSegmentRequest(string Name, Guid? RequiredTagId, decimal? MinimumLifetimeValue,
    decimal? MaximumLifetimeValue, int? MinimumSalesCount, int? PurchasedWithinDays, bool ActiveCustomersOnly, long ExpectedRevision);
public sealed record CustomerSegmentDto(Guid Id, string Name, Guid? RequiredTagId, string? RequiredTagName,
    decimal? MinimumLifetimeValue, decimal? MaximumLifetimeValue, int? MinimumSalesCount, int? PurchasedWithinDays,
    bool ActiveCustomersOnly, long Revision, DateTimeOffset CreatedAtUtc);
public sealed record CustomerSegmentMembersDto(CustomerSegmentDto Segment, int TotalCount, bool IsTruncated,
    IReadOnlyList<CustomerDto> Customers);
