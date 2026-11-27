using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ICustomerSegmentRepository
{
    Task<IReadOnlyList<CustomerSegmentDto>> GetSegmentsAsync(CancellationToken cancellationToken = default);
    Task<CustomerSegmentMembersDto> GetMembersAsync(Guid segmentId, int limit = 200, CancellationToken cancellationToken = default);
    Task<CustomerSegmentDto> CreateAsync(CreateCustomerSegmentRequest request, CancellationToken cancellationToken = default);
    Task<CustomerSegmentDto> UpdateAsync(Guid segmentId, UpdateCustomerSegmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid segmentId, long expectedRevision, CancellationToken cancellationToken = default);
}
