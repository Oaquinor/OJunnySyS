using OJunnySyS.Domain.Accounting;

namespace OJunnySyS.Application.Contracts;

public sealed record ChartAccountDto(Guid Id, string Code, string Name, AccountType Type, string? Description, bool IsSystem, bool IsActive,
    long Revision, decimal DebitBalance, decimal CreditBalance);
public sealed record CreateChartAccountRequest(string Code, string Name, AccountType Type, string? Description = null);
public sealed record SetChartAccountActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record JournalEntryLineRequest(Guid AccountId, decimal Debit, decimal Credit, string? Memo = null);
public sealed record CreateJournalEntryRequest(DateOnly EntryDate, string Description, IReadOnlyCollection<JournalEntryLineRequest> Lines);
public sealed record JournalEntryLineDto(Guid AccountId, string AccountCode, string AccountName, decimal Debit, decimal Credit, string? Memo);
public sealed record JournalEntryDto(Guid Id, string Number, DateOnly EntryDate, string Description, JournalEntryStatus Status,
    decimal TotalDebit, decimal TotalCredit, DateTimeOffset CreatedAtUtc, IReadOnlyList<JournalEntryLineDto> Lines);
public sealed record TrialBalanceLineDto(Guid AccountId, string Code, string Name, AccountType Type, decimal Debit, decimal Credit);
public sealed record TrialBalanceDto(DateOnly AsOfDate, IReadOnlyList<TrialBalanceLineDto> Lines, decimal TotalDebit, decimal TotalCredit);
