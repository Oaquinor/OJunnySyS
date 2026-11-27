using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public sealed class BankingService(IBankingRepository repository, IBusinessClock clock)
{
    private const int MaximumStatementLines = 5000;
    private const int MaximumPeriodDays = 366;

    public Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default) =>
        repository.GetBankAccountsAsync(ct);

    public Task<BankAccountDto> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken ct = default) =>
        repository.CreateBankAccountAsync(request, ct);

    public Task<BankAccountDto> UpdateBankAccountAsync(Guid id, UpdateBankAccountRequest request, CancellationToken ct = default) =>
        repository.UpdateBankAccountAsync(id, request, ct);

    public Task<BankAccountDto> SetBankAccountActiveAsync(Guid id, SetBankAccountActiveRequest request, CancellationToken ct = default) =>
        repository.SetBankAccountActiveAsync(id, request, ct);

    public Task<IReadOnlyList<BankReconciliationListItemDto>> GetReconciliationsAsync(Guid? bankAccountId = null, CancellationToken ct = default) =>
        repository.GetReconciliationsAsync(bankAccountId, ct);

    public Task<BankReconciliationDto> GetReconciliationAsync(Guid id, CancellationToken ct = default) =>
        repository.GetReconciliationAsync(id, ct);

    public async Task<BankReconciliationDto> CreateReconciliationAsync(CreateBankReconciliationRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        if (request.PeriodStart > request.PeriodEnd || request.PeriodEnd > today ||
            request.PeriodEnd.DayNumber - request.PeriodStart.DayNumber >= MaximumPeriodDays)
            throw new DomainException("El período debe ser válido, no futuro y tener como máximo 366 días.");
        if (request.Lines is null || request.Lines.Count > MaximumStatementLines)
            throw new DomainException($"El estado bancario admite hasta {MaximumStatementLines:N0} movimientos.");
        if (request.Lines.Any(x => x.TransactionDate < request.PeriodStart || x.TransactionDate > request.PeriodEnd))
            throw new DomainException("Cada movimiento debe estar dentro del período del estado bancario.");
        if (request.Lines.Any(x => x.Amount != decimal.Round(x.Amount, 2) || x.Amount == 0))
            throw new DomainException("Cada movimiento debe tener un importe distinto de cero y hasta dos decimales.");
        if (request.OpeningBalance + request.Lines.Sum(x => x.Amount) != request.ClosingBalance)
            throw new DomainException("El balance final no coincide con el balance inicial más los movimientos del estado.");

        return await repository.CreateReconciliationAsync(request, ct);
    }

    public Task<BankReconciliationDto> MatchStatementLineAsync(Guid reconciliationId, Guid statementLineId,
        Guid journalEntryLineId, CancellationToken ct = default) =>
        repository.MatchStatementLineAsync(reconciliationId, statementLineId, journalEntryLineId, ct);

    public Task<BankReconciliationDto> UnmatchStatementLineAsync(Guid reconciliationId, Guid statementLineId,
        CancellationToken ct = default) => repository.UnmatchStatementLineAsync(reconciliationId, statementLineId, ct);

    public Task<BankReconciliationDto> CompleteReconciliationAsync(Guid id, CancellationToken ct = default) =>
        repository.CompleteReconciliationAsync(id, ct);
}
