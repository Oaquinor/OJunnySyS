using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public sealed class AccountingService(IAccountingRepository repository, IBusinessClock clock)
{
    public Task<IReadOnlyList<ChartAccountDto>> GetChartOfAccountsAsync(DateOnly? asOfDate = null, CancellationToken ct = default) =>
        repository.GetChartOfAccountsAsync(asOfDate, ct);

    public Task<ChartAccountDto> CreateAccountAsync(CreateChartAccountRequest request, CancellationToken ct = default) =>
        repository.CreateAccountAsync(request, ct);

    public Task<ChartAccountDto> SetAccountActiveAsync(Guid id, SetChartAccountActiveRequest request, CancellationToken ct = default) =>
        repository.SetAccountActiveAsync(id, request, ct);

    public async Task<JournalEntryDto> CreateJournalEntryAsync(CreateJournalEntryRequest request, Guid actorId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        if (request.EntryDate > today) throw new DomainException("La fecha del asiento no puede estar en el futuro.");
        return await repository.CreateJournalEntryAsync(request, actorId, ct);
    }

    public Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(DateOnly? from = null, DateOnly? to = null, int take = 100, CancellationToken ct = default)
    {
        if (from is { } start && to is { } end && start > end) throw new DomainException("El período del libro diario no es válido.");
        return repository.GetJournalEntriesAsync(from, to, take, ct);
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOfDate, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        if (asOfDate > today) throw new DomainException("La fecha del balance de comprobación no puede estar en el futuro.");
        return await repository.GetTrialBalanceAsync(asOfDate, ct);
    }

    public async Task<ProfitAndLossDto> GetProfitAndLossAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        if (from > to || to > today || to.DayNumber - from.DayNumber > 365)
            throw new DomainException("El período del estado de resultados debe ser válido y no superar 366 días.");
        var activity = await repository.GetTrialBalanceBetweenAsync(from, to, ct);
        var revenue = activity.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Revenue)
            .Select(x => new FinancialStatementLineDto(x.Code, x.Name, x.Type, x.Credit - x.Debit)).Where(x => x.Amount != 0).ToArray();
        var expenses = activity.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Expense)
            .Select(x => new FinancialStatementLineDto(x.Code, x.Name, x.Type, x.Debit - x.Credit)).Where(x => x.Amount != 0).ToArray();
        var totalRevenue = revenue.Sum(x => x.Amount);
        var totalExpenses = expenses.Sum(x => x.Amount);
        return new ProfitAndLossDto(from, to, revenue, expenses, totalRevenue, totalExpenses, totalRevenue - totalExpenses);
    }

    public async Task<FinancialPositionDto> GetFinancialPositionAsync(DateOnly asOfDate, CancellationToken ct = default)
    {
        var trial = await GetTrialBalanceAsync(asOfDate, ct);
        decimal DebitBalance(TrialBalanceLineDto x) => x.Debit - x.Credit;
        decimal CreditBalance(TrialBalanceLineDto x) => x.Credit - x.Debit;
        var assets = trial.Lines.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Asset)
            .Select(x => new FinancialStatementLineDto(x.Code, x.Name, x.Type, DebitBalance(x))).Where(x => x.Amount != 0).ToArray();
        var liabilities = trial.Lines.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Liability)
            .Select(x => new FinancialStatementLineDto(x.Code, x.Name, x.Type, CreditBalance(x))).Where(x => x.Amount != 0).ToArray();
        var equity = trial.Lines.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Equity)
            .Select(x => new FinancialStatementLineDto(x.Code, x.Name, x.Type, CreditBalance(x))).Where(x => x.Amount != 0).ToArray();
        var result = trial.Lines.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Revenue).Sum(CreditBalance)
            - trial.Lines.Where(x => x.Type == OJunnySyS.Domain.Accounting.AccountType.Expense).Sum(DebitBalance);
        return new FinancialPositionDto(asOfDate, assets, liabilities, equity, assets.Sum(x => x.Amount),
            liabilities.Sum(x => x.Amount), equity.Sum(x => x.Amount) + result, result);
    }
}
