using System.Data;
using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Domain.Banking;
using OJunnySyS.Domain.Common;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfBankingRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    IBusinessClock clock) : IBankingRepository
{
    public async Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        var accounts = await (from bank in db.BankAccounts.AsNoTracking()
                              join ledger in db.ChartAccounts.AsNoTracking() on new { bank.TenantId, Id = bank.LedgerAccountId } equals new { ledger.TenantId, ledger.Id }
                              orderby bank.Name
                              select new { bank, LedgerCode = ledger.Code, LedgerName = ledger.Name }).ToListAsync(ct);
        var ledgerIds = accounts.Select(x => x.bank.LedgerAccountId).Distinct().ToArray();
        var today = DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        var balances = await GetBookBalancesAsync(db, ledgerIds, today, ct);
        return accounts.Select(x => ToBankAccountDto(x.bank, x.LedgerCode, x.LedgerName, balances.GetValueOrDefault(x.bank.LedgerAccountId))).ToArray();
    }

    public async Task<BankAccountDto> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(ct);
        var ledger = await db.ChartAccounts.SingleOrDefaultAsync(x => x.Id == request.LedgerAccountId && x.IsActive, ct)
            ?? throw new DomainException("Selecciona una cuenta contable activa de esta empresa.");
        ValidateBankLedger(ledger);
        await EnsureTenantCurrencyAsync(db, tenantId, request.CurrencyCode, ct);
        if (await db.BankAccounts.AnyAsync(x => x.LedgerAccountId == request.LedgerAccountId, ct))
            throw new DomainException("Cada cuenta bancaria debe tener una cuenta contable propia para poder conciliarla.");
        var account = new BankAccount(tenantId, request.Name, request.Institution, request.AccountSuffix, request.CurrencyCode, request.LedgerAccountId);
        db.BankAccounts.Add(account);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { throw new DomainException("Ya existe una cuenta bancaria con ese nombre o cuenta contable."); }
        var balances = await GetBookBalancesAsync(db, [account.LedgerAccountId], DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime), ct);
        return ToBankAccountDto(account, ledger.Code, ledger.Name, balances.GetValueOrDefault(account.LedgerAccountId));
    }

    public async Task<BankAccountDto> UpdateBankAccountAsync(Guid id, UpdateBankAccountRequest request, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(ct);
        var account = await db.BankAccounts.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("La cuenta bancaria no existe en la empresa activa.");
        var ledger = await db.ChartAccounts.SingleOrDefaultAsync(x => x.Id == request.LedgerAccountId && x.IsActive, ct)
            ?? throw new DomainException("Selecciona una cuenta contable activa de esta empresa.");
        ValidateBankLedger(ledger);
        await EnsureTenantCurrencyAsync(db, tenantId, request.CurrencyCode, ct);
        if (account.LedgerAccountId != request.LedgerAccountId)
        {
            if (await db.BankAccounts.AnyAsync(x => x.Id != id && x.LedgerAccountId == request.LedgerAccountId, ct))
                throw new DomainException("Cada cuenta bancaria debe tener una cuenta contable propia.");
            if (await db.BankReconciliations.AnyAsync(x => x.BankAccountId == id, ct) ||
                await db.JournalEntryLines.AnyAsync(x => x.AccountId == account.LedgerAccountId, ct))
                throw new DomainException("No se puede cambiar la cuenta contable después de registrar movimientos o estados bancarios.");
        }
        account.Update(request.Name, request.Institution, request.AccountSuffix, request.CurrencyCode, request.LedgerAccountId, request.ExpectedRevision);
        await SaveWithConcurrencyAsync(db, ct, "La cuenta bancaria cambió en otra sesión. Recarga antes de guardar.");
        var balance = await GetBookBalancesAsync(db, [account.LedgerAccountId], DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime), ct);
        return ToBankAccountDto(account, ledger.Code, ledger.Name, balance.GetValueOrDefault(account.LedgerAccountId));
    }

    public async Task<BankAccountDto> SetBankAccountActiveAsync(Guid id, SetBankAccountActiveRequest request, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        var account = await db.BankAccounts.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("La cuenta bancaria no existe en la empresa activa.");
        if (!request.IsActive && await db.BankReconciliations.AnyAsync(x => x.BankAccountId == id && x.Status == BankReconciliationStatus.Open, ct))
            throw new DomainException("Termina o corrige las conciliaciones abiertas antes de desactivar la cuenta.");
        account.SetActive(request.IsActive, request.ExpectedRevision);
        await SaveWithConcurrencyAsync(db, ct, "La cuenta bancaria cambió en otra sesión. Recarga antes de guardar.");
        var ledger = await db.ChartAccounts.SingleAsync(x => x.Id == account.LedgerAccountId, ct);
        var balances = await GetBookBalancesAsync(db, [account.LedgerAccountId], DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime), ct);
        return ToBankAccountDto(account, ledger.Code, ledger.Name, balances.GetValueOrDefault(account.LedgerAccountId));
    }

    public async Task<IReadOnlyList<BankReconciliationListItemDto>> GetReconciliationsAsync(Guid? bankAccountId = null, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        var query = from statement in db.BankReconciliations.AsNoTracking()
                    join account in db.BankAccounts.AsNoTracking() on new { statement.TenantId, Id = statement.BankAccountId } equals new { account.TenantId, account.Id }
                    select new { statement, account.Name };
        if (bankAccountId is { } id) query = query.Where(x => x.statement.BankAccountId == id);
        var rows = await query.OrderByDescending(x => x.statement.PeriodEnd).ThenByDescending(x => x.statement.CreatedAtUtc).Take(500).ToListAsync(ct);
        var statementIds = rows.Select(x => x.statement.Id).ToArray();
        var counts = await db.BankStatementLines.AsNoTracking().Where(x => statementIds.Contains(x.BankReconciliationId))
            .GroupBy(x => x.BankReconciliationId)
            .Select(g => new { Id = g.Key, Total = g.Count(), Matched = g.Count(x => x.JournalEntryLineId != null) })
            .ToDictionaryAsync(x => x.Id, ct);
        return rows.Select(x =>
        {
            var count = counts.GetValueOrDefault(x.statement.Id);
            return new BankReconciliationListItemDto(x.statement.Id, x.statement.BankAccountId, x.Name,
                x.statement.StatementNumber, x.statement.PeriodStart, x.statement.PeriodEnd, x.statement.OpeningBalance,
                x.statement.ClosingBalance, x.statement.Status, count?.Total ?? 0, count?.Matched ?? 0,
                x.statement.CreatedAtUtc);
        }).ToArray();
    }

    public async Task<BankReconciliationDto> CreateReconciliationAsync(CreateBankReconciliationRequest request, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(ct);
        var account = await db.BankAccounts.SingleOrDefaultAsync(x => x.Id == request.BankAccountId && x.IsActive, ct)
            ?? throw new DomainException("Selecciona una cuenta bancaria activa de esta empresa.");
        if (await db.BankReconciliations.AnyAsync(x => x.BankAccountId == account.Id &&
            x.StatementNumber == request.StatementNumber.Trim(), ct))
            throw new DomainException("Ya existe un estado bancario con esa referencia.");
        if (await db.BankReconciliations.AnyAsync(x => x.BankAccountId == account.Id &&
            x.PeriodStart <= request.PeriodEnd && x.PeriodEnd >= request.PeriodStart, ct))
            throw new DomainException("El período se cruza con un estado bancario ya registrado para esta cuenta.");

        var statement = new BankReconciliation(tenantId, account.Id, request.StatementNumber, request.PeriodStart,
            request.PeriodEnd, request.OpeningBalance, request.ClosingBalance);
        foreach (var line in request.Lines)
            statement.AddLine(line.TransactionDate, line.Description, line.Amount, line.Reference);
        db.BankReconciliations.Add(statement);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { throw new DomainException("La referencia o el período del estado bancario ya existe."); }
        return await BuildReconciliationDtoAsync(db, statement, account, ct);
    }

    public async Task<BankReconciliationDto> GetReconciliationAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        var statement = await db.BankReconciliations.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("La conciliación no existe en la empresa activa.");
        var account = await db.BankAccounts.SingleAsync(x => x.Id == statement.BankAccountId, ct);
        return await BuildReconciliationDtoAsync(db, statement, account, ct);
    }

    public async Task<BankReconciliationDto> MatchStatementLineAsync(Guid reconciliationId, Guid statementLineId,
        Guid journalEntryLineId, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var statement = await db.BankReconciliations.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == reconciliationId, ct)
            ?? throw new DomainException("La conciliación no existe en la empresa activa.");
        statement.EnsureOpen();
        var line = statement.Lines.SingleOrDefault(x => x.Id == statementLineId)
            ?? throw new DomainException("El movimiento no pertenece a esta conciliación.");
        var account = await db.BankAccounts.SingleAsync(x => x.Id == statement.BankAccountId, ct);
        var candidate = await (from bookLine in db.JournalEntryLines.AsNoTracking()
                               join entry in db.JournalEntries.AsNoTracking() on new { bookLine.TenantId, Id = bookLine.JournalEntryId } equals new { entry.TenantId, entry.Id }
                               where bookLine.Id == journalEntryLineId && bookLine.AccountId == account.LedgerAccountId &&
                                     entry.Status == JournalEntryStatus.Posted && entry.EntryDate <= statement.PeriodEnd
                               select new { bookLine.Id, Amount = bookLine.Debit - bookLine.Credit }).SingleOrDefaultAsync(ct)
            ?? throw new DomainException("El movimiento contable no corresponde a la cuenta bancaria y fecha de corte seleccionadas.");
        if (candidate.Amount != line.Amount)
            throw new DomainException("Los importes no coinciden. Selecciona el movimiento contable con el mismo signo e importe.");
        if (await db.BankStatementLines.AnyAsync(x => x.JournalEntryLineId == candidate.Id, ct))
            throw new DomainException("Ese movimiento contable ya fue conciliado en otro estado bancario.");
        statement.MatchLine(statementLineId, journalEntryLineId);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("La conciliación cambió en otra sesión. Recarga y vuelve a intentar."); }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { throw new DomainException("Ese movimiento contable acaba de ser conciliado en otra sesión."); }
        return await BuildReconciliationDtoAsync(db, statement, account, ct);
    }

    public async Task<BankReconciliationDto> UnmatchStatementLineAsync(Guid reconciliationId, Guid statementLineId, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        var statement = await db.BankReconciliations.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == reconciliationId, ct)
            ?? throw new DomainException("La conciliación no existe en la empresa activa.");
        var account = await db.BankAccounts.SingleAsync(x => x.Id == statement.BankAccountId, ct);
        statement.UnmatchLine(statementLineId);
        await SaveWithConcurrencyAsync(db, ct, "La conciliación cambió en otra sesión. Recarga antes de guardar.");
        return await BuildReconciliationDtoAsync(db, statement, account, ct);
    }

    public async Task<BankReconciliationDto> CompleteReconciliationAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var statement = await db.BankReconciliations.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new DomainException("La conciliación no existe en la empresa activa.");
        var account = await db.BankAccounts.SingleAsync(x => x.Id == statement.BankAccountId, ct);
        var result = await BuildReconciliationDtoAsync(db, statement, account, ct);
        if (!result.IsBalanced)
            throw new DomainException($"No se puede cerrar: después de considerar movimientos pendientes queda una diferencia de {result.Difference:N2} {account.CurrencyCode}.");
        statement.MarkReconciled(clock.UtcNow);
        await SaveWithConcurrencyAsync(db, ct, "La conciliación cambió en otra sesión. Recarga antes de cerrar.");
        await transaction.CommitAsync(ct);
        return result with { Status = BankReconciliationStatus.Reconciled, ReconciledAtUtc = statement.ReconciledAtUtc };
    }

    private async Task<BankReconciliationDto> BuildReconciliationDtoAsync(StoreDbContext db, BankReconciliation statement,
        BankAccount account, CancellationToken ct)
    {
        if (db.Entry(statement).Collection(x => x.Lines).IsLoaded == false)
            await db.Entry(statement).Collection(x => x.Lines).LoadAsync(ct);
        var accountLedger = await db.ChartAccounts.AsNoTracking().SingleAsync(x => x.Id == account.LedgerAccountId, ct);
        var bookRows = await (from bookLine in db.JournalEntryLines.AsNoTracking()
                              join entry in db.JournalEntries.AsNoTracking() on new { bookLine.TenantId, Id = bookLine.JournalEntryId } equals new { entry.TenantId, entry.Id }
                              where bookLine.AccountId == account.LedgerAccountId && entry.Status == JournalEntryStatus.Posted && entry.EntryDate <= statement.PeriodEnd
                              select new { bookLine.Id, bookLine.Debit, bookLine.Credit, entry.EntryDate, entry.Number, entry.Description, bookLine.Memo })
            .OrderBy(x => x.EntryDate).ThenBy(x => x.Number).ToListAsync(ct);
        var matchedBookIds = await db.BankStatementLines.AsNoTracking().Where(x => x.JournalEntryLineId != null)
            .Select(x => x.JournalEntryLineId!.Value).ToHashSetAsync(ct);
        var matchedBook = bookRows.Where(x => matchedBookIds.Contains(x.Id))
            .Select(x => new BookMovementDto(x.Id, x.EntryDate, x.Number, x.Description, x.Memo, x.Debit - x.Credit)).ToArray();
        var unmatchedBook = bookRows.Where(x => !matchedBookIds.Contains(x.Id))
            .Select(x => new BookMovementDto(x.Id, x.EntryDate, x.Number, x.Description, x.Memo, x.Debit - x.Credit)).ToArray();
        var bookBalance = bookRows.Sum(x => x.Debit - x.Credit);
        var outstandingBalance = unmatchedBook.Sum(x => x.Amount);
        var adjustedStatementBalance = statement.ClosingBalance + outstandingBalance;
        var lineDtos = statement.Lines.OrderBy(x => x.TransactionDate).ThenBy(x => x.CreatedAtUtc)
            .Select(x => new BankStatementLineDto(x.Id, x.TransactionDate, x.Description, x.Amount, x.Reference, x.JournalEntryLineId)).ToArray();
        var accountName = await db.BankAccounts.AsNoTracking().Where(x => x.Id == account.Id).Select(x => x.Name).SingleAsync(ct);
        return new BankReconciliationDto(statement.Id, account.Id, accountName, account.CurrencyCode, account.LedgerAccountId,
            accountLedger.Code,
            statement.StatementNumber, statement.PeriodStart, statement.PeriodEnd, statement.OpeningBalance, statement.ClosingBalance,
            statement.Status, statement.ReconciledAtUtc, lineDtos, matchedBook, unmatchedBook, bookBalance, outstandingBalance,
            adjustedStatementBalance, bookBalance - adjustedStatementBalance, lineDtos.Count(x => x.JournalEntryLineId != null));
    }

    private async Task<Dictionary<Guid, decimal>> GetBookBalancesAsync(StoreDbContext db, Guid[] accountIds, DateOnly asOf, CancellationToken ct)
    {
        if (accountIds.Length == 0) return [];
        var postedLines = await (from line in db.JournalEntryLines.AsNoTracking()
                                 join entry in db.JournalEntries.AsNoTracking() on new { line.TenantId, Id = line.JournalEntryId } equals new { entry.TenantId, entry.Id }
                                 where accountIds.Contains(line.AccountId) && entry.Status == JournalEntryStatus.Posted && entry.EntryDate <= asOf
                                 select new { line.AccountId, line.Debit, line.Credit }).ToListAsync(ct);
        return postedLines.GroupBy(x => x.AccountId)
            .ToDictionary(group => group.Key, group => group.Sum(x => x.Debit - x.Credit));
    }

    private async Task EnsureTenantCurrencyAsync(StoreDbContext db, Guid tenantId, string currencyCode, CancellationToken ct)
    {
        var tenantCurrency = await db.Tenants.Where(x => x.Id == tenantId).Select(x => x.CurrencyCode).SingleAsync(ct);
        if (!string.Equals(tenantCurrency, currencyCode?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"La cuenta bancaria debe usar la moneda contable de la empresa ({tenantCurrency}) hasta que se habilite la contabilidad multimoneda.");
    }

    private static void ValidateBankLedger(ChartAccount account)
    {
        if (account.Type != AccountType.Asset || !account.Code.StartsWith("10", StringComparison.Ordinal))
            throw new DomainException("La cuenta vinculada debe ser una cuenta activa de efectivo o banco (código del grupo 10).");
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken ct)
    {
        var db = await contextFactory.CreateDbContextAsync(ct);
        db.CurrentTenantId = tenantContext.GetRequiredTenantId();
        return db;
    }

    private static BankAccountDto ToBankAccountDto(BankAccount account, string code, string ledgerName, decimal balance) =>
        new(account.Id, account.Name, account.Institution, account.AccountSuffix, account.CurrencyCode, account.LedgerAccountId,
            code, ledgerName, account.IsActive, account.Revision, balance);

    private static async Task SaveWithConcurrencyAsync(StoreDbContext db, CancellationToken ct, string conflictMessage)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(conflictMessage); }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}


