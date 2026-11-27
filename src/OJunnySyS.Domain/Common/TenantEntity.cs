namespace OJunnySyS.Domain.Common;

public abstract class TenantEntity : Entity
{
    protected TenantEntity() { }

    protected TenantEntity(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new DomainException("El identificador de empresa es obligatorio.");
        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }
}
