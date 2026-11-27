using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Banking;

public enum BankReconciliationStatus { Open, Reconciled }

/// <summary>An immutable-period bank statement reconciliation; its lines are imported from the statement.</summary>
public sealed class BankReconciliation : TenantEntity
{
    private readonly List<BankStatementLine> _lines = [];
    private BankReconciliation() { }

    public BankReconciliation(Guid tenantId, Guid bankAccountId, string statementNumber, DateOnly periodStart,
        DateOnly periodEnd, decimal openingBalance, decimal closingBalance) : base(tenantId)
    {
        if (bankAccountId == Guid.Empty) throw new DomainException("Selecciona una cuenta bancaria.");
        BankAccountId = bankAccountId;
        StatementNumber = NormalizeStatementNumber(statementNumber);
        if (periodStart > periodEnd) throw new DomainException("El período del estado bancario no es válido.");
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        OpeningBalance = NormalizeAmount(openingBalance, "El balance inicial");
        ClosingBalance = NormalizeAmount(closingBalance, "El balance final");
        Status = BankReconciliationStatus.Open;
    }

    public Guid BankAccountId { get; private set; }
    public string StatementNumber { get; private set; } = string.Empty;
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public BankReconciliationStatus Status { get; private set; }
    public DateTimeOffset? ReconciledAtUtc { get; private set; }
    public long Revision { get; private set; }
    public IReadOnlyCollection<BankStatementLine> Lines => _lines;

    public BankStatementLine AddLine(DateOnly transactionDate, string description, decimal amount, string? reference = null)
    {
        EnsureOpen();
        if (transactionDate < PeriodStart || transactionDate > PeriodEnd)
            throw new DomainException("La fecha del movimiento está fuera del período del estado bancario.");
        var line = new BankStatementLine(TenantId, Id, transactionDate, description, amount, reference);
        _lines.Add(line);
        Revision = checked(Revision + 1);
        MarkUpdated();
        return line;
    }

    public void MatchLine(Guid statementLineId, Guid journalEntryLineId)
    {
        EnsureOpen();
        var line = _lines.SingleOrDefault(x => x.Id == statementLineId)
            ?? throw new DomainException("El movimiento no pertenece a esta conciliación.");
        line.Match(journalEntryLineId);
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    public void UnmatchLine(Guid statementLineId)
    {
        EnsureOpen();
        var line = _lines.SingleOrDefault(x => x.Id == statementLineId)
            ?? throw new DomainException("El movimiento no pertenece a esta conciliación.");
        line.Unmatch();
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    public void EnsureOpen()
    {
        if (Status != BankReconciliationStatus.Open) throw new DomainException("La conciliación ya está cerrada.");
    }

    public void MarkReconciled(DateTimeOffset completedAtUtc)
    {
        EnsureOpen();
        if (_lines.Any(x => !x.IsMatched)) throw new DomainException("Concilia todos los movimientos del estado bancario antes de cerrarlo.");
        Status = BankReconciliationStatus.Reconciled;
        ReconciledAtUtc = completedAtUtc.ToUniversalTime();
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    private static string NormalizeStatementNumber(string? number)
    {
        number = number?.Trim() ?? string.Empty;
        if (number.Length is < 1 or > 80) throw new DomainException("La referencia del estado debe tener entre 1 y 80 caracteres.");
        return number;
    }

    private static decimal NormalizeAmount(decimal amount, string label)
    {
        if (amount != decimal.Round(amount, 2)) throw new DomainException($"{label} debe tener hasta dos decimales.");
        return amount;
    }
}
