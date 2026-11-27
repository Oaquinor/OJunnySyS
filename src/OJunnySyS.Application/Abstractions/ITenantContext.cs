namespace OJunnySyS.Application.Abstractions;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    Guid? CurrentUserId { get; }
    Guid GetRequiredTenantId();
    void SetTenantId(Guid? tenantId);
}
