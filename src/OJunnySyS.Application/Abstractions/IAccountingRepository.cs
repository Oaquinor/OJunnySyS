using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Abstractions;

public interface IAccountingRepository
{
    Task<IReadOnlyList<ChartAccountDto>> GetChartOfAccountsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<ChartAccountDto> CreateAccountAsync(CreateChartAccountRequest request, CancellationToken cancellationToken = default);
    Task<ChartAccountDto> SetAccountActiveAsync(Guid accountId, SetChartAccountActiveRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> CreateJournalEntryAsync(CreateJournalEntryRequest request, Guid postedByUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(DateOnly? from = null, DateOnly? to = null, int take = 100, CancellationToken cancellationToken = default);
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);
}
