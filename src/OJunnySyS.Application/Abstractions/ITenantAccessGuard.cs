namespace OJunnySyS.Application.Abstractions;

public interface ITenantAccessGuard
{
    Task EnsureCanOperateAsync(Guid tenantId, Guid? userId, CancellationToken cancellationToken = default);
}
