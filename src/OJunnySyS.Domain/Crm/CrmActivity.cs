using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Crm;

public enum CrmActivityKind { Note, Call, Email, Meeting, Task }

public sealed class CrmActivity : TenantEntity
{
    private CrmActivity() { }

    public CrmActivity(Guid tenantId, Guid customerId, Guid? opportunityId, Guid createdByUserId,
        CrmActivityKind kind, string subject, string? details, DateTimeOffset occurredAtUtc, DateTimeOffset? dueAtUtc, Guid? assignedToUserId = null)
        : base(tenantId)
    {
        if (customerId == Guid.Empty) throw new DomainException("Selecciona un cliente para la actividad.");
        if (createdByUserId == Guid.Empty) throw new DomainException("No se pudo identificar al usuario.");
        CustomerId = customerId;
        OpportunityId = opportunityId;
        CreatedByUserId = createdByUserId;
        AssignedToUserId = assignedToUserId;
        Kind = kind;
        Update(subject, details, occurredAtUtc, dueAtUtc);
    }

    public Guid CustomerId { get; private set; }
    public Guid? OpportunityId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public CrmActivityKind Kind { get; private set; }
    public string Subject { get; private set; } = "";
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset? DueAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public bool IsCompleted => CompletedAtUtc is not null;

    public void Update(string subject, string? details, DateTimeOffset occurredAtUtc, DateTimeOffset? dueAtUtc)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new DomainException("El asunto de la actividad es obligatorio.");
        if (subject.Trim().Length > 160) throw new DomainException("El asunto no puede exceder 160 caracteres.");
        if (details?.Length > 2000) throw new DomainException("Los detalles no pueden exceder 2,000 caracteres.");
        Subject = subject.Trim();
        Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        OccurredAtUtc = occurredAtUtc;
        DueAtUtc = dueAtUtc;
        MarkUpdated();
    }

    public void Complete(DateTimeOffset now)
    {
        if (Kind != CrmActivityKind.Task) throw new DomainException("Solo las tareas pueden marcarse como completadas.");
        if (IsCompleted) return;
        CompletedAtUtc = now;
        MarkUpdated();
    }
}
