using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Crm;

public enum OpportunityStage { Qualification, Proposal, Negotiation, Won, Lost }

public sealed class Opportunity : TenantEntity
{
    private Opportunity() { }

    public Opportunity(Guid tenantId, Guid customerId, string title, decimal expectedValue, DateOnly? expectedCloseDate, string? description, Guid? assignedToUserId = null)
        : base(tenantId)
    {
        CustomerId = customerId;
        AssignedToUserId = assignedToUserId;
        UpdateDetails(title, expectedValue, expectedCloseDate, description);
        Stage = OpportunityStage.Qualification;
    }

    public Guid CustomerId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string Title { get; private set; } = "";
    public decimal ExpectedValue { get; private set; }
    public DateOnly? ExpectedCloseDate { get; private set; }
    public string? Description { get; private set; }
    public OpportunityStage Stage { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public void UpdateDetails(string title, decimal expectedValue, DateOnly? expectedCloseDate, string? description)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("El nombre de la oportunidad es obligatorio.");
        if (title.Trim().Length > 160) throw new DomainException("El nombre de la oportunidad no puede exceder 160 caracteres.");
        if (expectedValue < 0) throw new DomainException("El valor esperado no puede ser negativo.");
        Title = title.Trim();
        ExpectedValue = decimal.Round(expectedValue, 2, MidpointRounding.AwayFromZero);
        ExpectedCloseDate = expectedCloseDate;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void MoveTo(OpportunityStage stage, DateTimeOffset now)
    {
        if (!Enum.IsDefined(stage)) throw new DomainException("La etapa de oportunidad no es válida.");
        if (Stage is OpportunityStage.Won or OpportunityStage.Lost) throw new DomainException("Una oportunidad cerrada no puede cambiar de etapa.");
        Stage = stage;
        ClosedAtUtc = stage is OpportunityStage.Won or OpportunityStage.Lost ? now : null;
        MarkUpdated();
    }
}
