using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Domain.Common;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfAccountingRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext) : IAccountingRepository
{
    public async Task<IReadOnlyList<ChartAccountDto>> GetChartOfAccountsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var accounts = await db.ChartAccounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var balances = await GetBalancesAsync(db, asOfDate, cancellationToken);
        return accounts.Select(x => ToAccountDto(x, balances.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<ChartAccountDto> CreateAccountAsync(CreateChartAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new ChartAccount(tenantContext.GetRequiredTenantId(), request.Code, request.Name, request.Type, description: request.Description);
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (await db.ChartAccounts.AnyAsync(x => x.Code == account.Code, cancellationToken))
            throw new DomainException("Ya existe una cuenta con ese código en esta empresa.");
        db.ChartAccounts.Add(account);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { throw new DomainException("Ya existe una cuenta con ese código en esta empresa."); }
        return ToAccountDto(account, (0m, 0m));
    }

    public async Task<ChartAccountDto> SetAccountActiveAsync(Guid accountId, SetChartAccountActiveRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var account = await db.ChartAccounts.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken)
            ?? throw new DomainException("La cuenta no existe en la empresa activa.");
        if (!request.IsActive && account.IsActive && await db.TenantPaymentMethods.AnyAsync(x => x.IsActive && x.LedgerAccountId == accountId, cancellationToken))
            throw new DomainException("Asigna primero otra cuenta a los medios de pago activos antes de desactivar esta cuenta.");
        account.SetActive(request.IsActive, request.ExpectedRevision);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("La cuenta cambió en otra sesión. Recarga antes de guardar."); }
        var balances = await GetBalancesAsync(db, null, cancellationToken);
        return ToAccountDto(account, balances.GetValueOrDefault(account.Id));
    }

    public async Task<JournalEntryDto> CreateJournalEntryAsync(CreateJournalEntryRequest request, Guid postedByUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == postedByUserId && x.TenantId == tenantId && (x.LockoutEnd == null || x.LockoutEnd <= now), cancellationToken)
            ?? throw new DomainException("El usuario que registra el asiento no está activo en esta empresa.");
        var accountIds = request.Lines.Select(x => x.AccountId).Distinct().ToArray();
        var accounts = await db.ChartAccounts.Where(x => accountIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken);
        if (accounts.Count != accountIds.Length) throw new DomainException("Todos los asientos deben usar cuentas activas de la empresa.");
        var lines = request.Lines.Select(x =>
        {
            var account = accounts[x.AccountId];
            return new JournalEntryLine(tenantId, account.Id, account.Code, account.Name, x.Debit, x.Credit, x.Memo);
        }).ToArray();
        var entryDate = request.EntryDate;
        var nextNumber = await db.JournalEntries.CountAsync(x => x.EntryDate == entryDate, cancellationToken) + 1;
        var number = $"JE-{entryDate:yyyyMMdd}-{nextNumber:0000}";
        var entry = new JournalEntry(tenantId, number, entryDate, request.Description, postedByUserId, lines);
        db.JournalEntries.Add(entry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConcurrencyConflictException("El número del asiento cambió en otra sesión. Vuelve a intentar.");
        }
        return ToJournalDto(entry);
    }

    public async Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(DateOnly? from = null, DateOnly? to = null, int take = 100, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var query = db.JournalEntries.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (from is { } start) query = query.Where(x => x.EntryDate >= start);
        if (to is { } end) query = query.Where(x => x.EntryDate <= end);
        var entries = await query.OrderByDescending(x => x.EntryDate).ThenByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);
        return entries.Select(ToJournalDto).ToList();
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var accounts = await db.ChartAccounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var balances = await GetBalancesAsync(db, asOfDate, cancellationToken);
        var lines = accounts.Select(account =>
        {
            var (debitBalance, creditBalance) = balances.GetValueOrDefault(account.Id);
            var netDebit = debitBalance - creditBalance;
            var debit = Math.Max(netDebit, 0m);
            var credit = Math.Max(-netDebit, 0m);
            return new TrialBalanceLineDto(account.Id, account.Code, account.Name, account.Type, debit, credit);
        }).Where(x => x.Debit != 0 || x.Credit != 0).ToList();
        return new TrialBalanceDto(asOfDate, lines, lines.Sum(x => x.Debit), lines.Sum(x => x.Credit));
    }

    public async Task<IReadOnlyList<TrialBalanceLineDto>> GetTrialBalanceBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var accounts = await db.ChartAccounts.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var balances = await db.JournalEntryLines.AsNoTracking()
            .Join(db.JournalEntries.AsNoTracking(), line => new { line.TenantId, line.JournalEntryId }, entry => new { entry.TenantId, JournalEntryId = entry.Id }, (line, entry) => new { line, entry })
            .Where(x => x.entry.Status == JournalEntryStatus.Posted && x.entry.EntryDate >= from && x.entry.EntryDate <= to)
            .GroupBy(x => x.line.AccountId)
            .Select(items => new { AccountId = items.Key, Debit = items.Sum(x => x.line.Debit), Credit = items.Sum(x => x.line.Credit) })
            .ToDictionaryAsync(x => x.AccountId, x => (x.Debit, x.Credit), cancellationToken);
        return accounts.Select(account =>
        {
            var (debitBalance, creditBalance) = balances.GetValueOrDefault(account.Id);
            var net = debitBalance - creditBalance;
            return new TrialBalanceLineDto(account.Id, account.Code, account.Name, account.Type, Math.Max(net, 0m), Math.Max(-net, 0m));
        }).Where(x => x.Debit != 0 || x.Credit != 0).ToArray();
    }

    private async Task<Dictionary<Guid, (decimal Debit, decimal Credit)>> GetBalancesAsync(StoreDbContext db, DateOnly? asOfDate, CancellationToken ct)
    {
        var query = from line in db.JournalEntryLines.AsNoTracking()
                    join entry in db.JournalEntries.AsNoTracking() on new { line.TenantId, line.JournalEntryId } equals new { entry.TenantId, JournalEntryId = entry.Id }
                    where entry.Status == JournalEntryStatus.Posted
                    select new { line.AccountId, line.Debit, line.Credit, entry.EntryDate };
        if (asOfDate is { } date) query = query.Where(x => x.EntryDate <= date);
        return await query.GroupBy(x => x.AccountId)
            .Select(group => new { AccountId = group.Key, Debit = group.Sum(x => x.Debit), Credit = group.Sum(x => x.Credit) })
            .ToDictionaryAsync(x => x.AccountId, x => (x.Debit, x.Credit), ct);
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken ct)
    {
        var db = await contextFactory.CreateDbContextAsync(ct);
        db.CurrentTenantId = tenantContext.GetRequiredTenantId();
        return db;
    }

    private static ChartAccountDto ToAccountDto(ChartAccount x, (decimal Debit, decimal Credit) balance) =>
        new(x.Id, x.Code, x.Name, x.Type, x.Description, x.IsSystem, x.IsActive, x.Revision, balance.Debit, balance.Credit);

    private static JournalEntryDto ToJournalDto(JournalEntry x) => new(x.Id, x.Number, x.EntryDate, x.Description, x.Status,
        x.TotalDebit, x.TotalCredit, x.CreatedAtUtc,
        x.Lines.OrderBy(line => line.AccountCode).Select(line => new JournalEntryLineDto(line.AccountId, line.AccountCode, line.AccountName, line.Debit, line.Credit, line.Memo)).ToList());

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}
