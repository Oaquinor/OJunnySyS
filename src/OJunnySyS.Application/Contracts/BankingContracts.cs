using OJunnySyS.Domain.Banking;

namespace OJunnySyS.Application.Contracts;

public sealed record BankAccountDto(Guid Id, string Name, string Institution, string? AccountSuffix, string CurrencyCode,
    Guid LedgerAccountId, string LedgerAccountCode, string LedgerAccountName, bool IsActive, long Revision, decimal BookBalance);
public sealed record CreateBankAccountRequest(string Name, string Institution, string? AccountSuffix, string CurrencyCode, Guid LedgerAccountId);
public sealed record UpdateBankAccountRequest(string Name, string Institution, string? AccountSuffix, string CurrencyCode,
    Guid LedgerAccountId, long ExpectedRevision);
public sealed record SetBankAccountActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record BankStatementLineRequest(DateOnly TransactionDate, string Description, decimal Amount, string? Reference = null);
public sealed record CreateBankReconciliationRequest(Guid BankAccountId, string StatementNumber, DateOnly PeriodStart,
    DateOnly PeriodEnd, decimal OpeningBalance, decimal ClosingBalance, IReadOnlyCollection<BankStatementLineRequest> Lines);
public sealed record BankStatementLineDto(Guid Id, DateOnly TransactionDate, string Description, decimal Amount,
    string? Reference, Guid? JournalEntryLineId);
public sealed record BookMovementDto(Guid JournalEntryLineId, DateOnly TransactionDate, string EntryNumber,
    string Description, string? Memo, decimal Amount);
public sealed record BankReconciliationListItemDto(Guid Id, Guid BankAccountId, string BankAccountName, string StatementNumber,
    DateOnly PeriodStart, DateOnly PeriodEnd, decimal OpeningBalance, decimal ClosingBalance,
    BankReconciliationStatus Status, int LineCount, int MatchedLineCount, DateTimeOffset CreatedAtUtc);
public sealed record BankReconciliationDto(Guid Id, Guid BankAccountId, string BankAccountName, string BankAccountCurrency,
    Guid LedgerAccountId, string LedgerAccountCode, string StatementNumber, DateOnly PeriodStart, DateOnly PeriodEnd, decimal OpeningBalance,
    decimal ClosingBalance, BankReconciliationStatus Status, DateTimeOffset? ReconciledAtUtc,
    IReadOnlyList<BankStatementLineDto> Lines, IReadOnlyList<BookMovementDto> MatchedBookMovements,
    IReadOnlyList<BookMovementDto> UnmatchedBookMovements,
    decimal BookBalance, decimal OutstandingBookBalance, decimal AdjustedStatementBalance, decimal Difference,
    int MatchedLineCount)
{
    public bool IsBalanced => Difference == 0m;
}
