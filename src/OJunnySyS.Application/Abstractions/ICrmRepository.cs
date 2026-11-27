using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Crm;

namespace OJunnySyS.Application.Abstractions;

public interface ICrmRepository
{
    Task<IReadOnlyList<OpportunityDto>> GetOpportunitiesAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityRequest request, CancellationToken cancellationToken = default);
    Task<OpportunityDto> MoveOpportunityAsync(Guid id, OpportunityStage stage, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CrmActivityDto>> GetActivitiesAsync(Guid? customerId = null, bool openTasksOnly = false, CancellationToken cancellationToken = default);
    Task<CrmActivityDto> CreateActivityAsync(CreateCrmActivityRequest request, Guid actorId, CancellationToken cancellationToken = default);
    Task<CrmActivityDto> CompleteTaskAsync(Guid id, CancellationToken cancellationToken = default);
}
