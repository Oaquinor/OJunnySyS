using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Customers;

public sealed class CustomerTagAssignment : TenantEntity
{
    private CustomerTagAssignment() { }

    public CustomerTagAssignment(Guid tenantId, Guid customerId, Guid tagId, Guid assignedByUserId) : base(tenantId)
    {
        if (customerId == Guid.Empty || tagId == Guid.Empty || assignedByUserId == Guid.Empty)
            throw new DomainException("Cliente, etiqueta y usuario asignador son obligatorios.");
        CustomerId = customerId;
        CustomerTagId = tagId;
        AssignedByUserId = assignedByUserId;
    }

    public Guid CustomerId { get; private set; }
    public Guid CustomerTagId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
}
