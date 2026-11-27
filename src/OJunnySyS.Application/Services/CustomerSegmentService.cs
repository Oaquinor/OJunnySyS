using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class CustomerSegmentService(ICustomerSegmentRepository repository)
{
    public Task<IReadOnlyList<CustomerSegmentDto>> GetSegmentsAsync(CancellationToken ct = default) => repository.GetSegmentsAsync(ct);
    public Task<CustomerSegmentMembersDto> GetMembersAsync(Guid id, int limit = 200, CancellationToken ct = default) => repository.GetMembersAsync(id, limit, ct);
    public Task<CustomerSegmentDto> CreateAsync(CreateCustomerSegmentRequest request, CancellationToken ct = default) => repository.CreateAsync(request, ct);
    public Task<CustomerSegmentDto> UpdateAsync(Guid id, UpdateCustomerSegmentRequest request, CancellationToken ct = default) => repository.UpdateAsync(id, request, ct);
    public Task DeleteAsync(Guid id, long expectedRevision, CancellationToken ct = default) => repository.DeleteAsync(id, expectedRevision, ct);
}
