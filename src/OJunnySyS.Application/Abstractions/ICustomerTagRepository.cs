using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface ICustomerTagRepository
{
    Task<IReadOnlyList<CustomerTagDto>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerTagAssignmentDto>> GetCustomerTagsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerTagAssignmentDto> AddToCustomerAsync(Guid customerId, AddCustomerTagRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task RemoveFromCustomerAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default);
}
