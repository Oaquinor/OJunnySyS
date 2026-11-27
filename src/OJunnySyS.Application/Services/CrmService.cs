using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Crm;

namespace OJunnySyS.Application.Services;

public sealed class CrmService(ICrmRepository repository)
{
    public Task<IReadOnlyList<OpportunityDto>> GetOpportunitiesAsync(Guid? customerId = null, CancellationToken ct = default) => repository.GetOpportunitiesAsync(customerId, ct);
    public Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityRequest request, CancellationToken ct = default) => repository.CreateOpportunityAsync(request, ct);
    public Task<OpportunityDto> MoveOpportunityAsync(Guid id, OpportunityStage stage, CancellationToken ct = default) => repository.MoveOpportunityAsync(id, stage, ct);
    public Task<IReadOnlyList<CrmActivityDto>> GetActivitiesAsync(Guid? customerId = null, bool openTasksOnly = false, CancellationToken ct = default) => repository.GetActivitiesAsync(customerId, openTasksOnly, ct);
    public Task<CrmActivityDto> CreateActivityAsync(CreateCrmActivityRequest request, Guid actorId, CancellationToken ct = default) => repository.CreateActivityAsync(request, actorId, ct);
    public Task<CrmActivityDto> CompleteTaskAsync(Guid id, CancellationToken ct = default) => repository.CompleteTaskAsync(id, ct);
}
