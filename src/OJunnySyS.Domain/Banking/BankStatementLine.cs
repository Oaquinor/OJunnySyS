using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Banking;

/// <summary>A signed movement imported from a bank statement. Positive is a deposit; negative is a withdrawal.</summary>
public sealed class BankStatementLine : TenantEntity
{
    private BankStatementLine() { }

    public BankStatementLine(Guid tenantId, Guid bankReconciliationId, DateOnly transactionDate, string description,
        decimal amount, string? reference = null) : base(tenantId)
    {
        if (bankReconciliationId == Guid.Empty) throw new DomainException("La línea debe pertenecer a una conciliación.");
        if (transactionDate == default) throw new DomainException("La fecha del movimiento es obligatoria.");
        description = description?.Trim() ?? string.Empty;
        if (description.Length is < 2 or > 180) throw new DomainException("La descripción debe tener entre 2 y 180 caracteres.");
        if (amount == 0 || amount != decimal.Round(amount, 2)) throw new DomainException("El movimiento debe ser distinto de cero y tener hasta dos decimales.");
        reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (reference?.Length > 120) throw new DomainException("La referencia no puede superar 120 caracteres.");
        BankReconciliationId = bankReconciliationId;
        TransactionDate = transactionDate;
        Description = description;
        Amount = amount;
        Reference = reference;
    }

    public Guid BankReconciliationId { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public Guid? JournalEntryLineId { get; private set; }
    public bool IsMatched => JournalEntryLineId is not null;

    public void Match(Guid journalEntryLineId)
    {
        if (journalEntryLineId == Guid.Empty) throw new DomainException("Selecciona un movimiento contable.");
        if (IsMatched) throw new DomainException("El movimiento bancario ya está conciliado.");
        JournalEntryLineId = journalEntryLineId;
        MarkUpdated();
    }

    public void Unmatch()
    {
        if (!IsMatched) return;
        JournalEntryLineId = null;
        MarkUpdated();
    }
}
