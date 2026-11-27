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
}
