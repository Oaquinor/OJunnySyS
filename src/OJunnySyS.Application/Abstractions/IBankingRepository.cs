using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface IBankingRepository
{
    Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default);
    Task<BankAccountDto> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken ct = default);
    Task<BankAccountDto> UpdateBankAccountAsync(Guid id, UpdateBankAccountRequest request, CancellationToken ct = default);
    Task<BankAccountDto> SetBankAccountActiveAsync(Guid id, SetBankAccountActiveRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<BankReconciliationListItemDto>> GetReconciliationsAsync(Guid? bankAccountId = null, CancellationToken ct = default);
    Task<BankReconciliationDto> CreateReconciliationAsync(CreateBankReconciliationRequest request, CancellationToken ct = default);
    Task<BankReconciliationDto> GetReconciliationAsync(Guid id, CancellationToken ct = default);
    Task<BankReconciliationDto> MatchStatementLineAsync(Guid reconciliationId, Guid statementLineId, Guid journalEntryLineId, CancellationToken ct = default);
    Task<BankReconciliationDto> UnmatchStatementLineAsync(Guid reconciliationId, Guid statementLineId, CancellationToken ct = default);
    Task<BankReconciliationDto> CompleteReconciliationAsync(Guid id, CancellationToken ct = default);
}
