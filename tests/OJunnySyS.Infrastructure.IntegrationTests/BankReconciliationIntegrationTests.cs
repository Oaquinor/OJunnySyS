using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Domain.Banking;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Infrastructure.Identity;
using OJunnySyS.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace OJunnySyS.Infrastructure.IntegrationTests;

[TestClass]
public sealed class BankReconciliationIntegrationTests
{
    [TestMethod]
    public async Task CashMovement_IsIdempotent_AndClosingVariancePostsBalancedEntry()
    {
        var connectionString = Environment.GetEnvironmentVariable("OJUNNYSYS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Inconclusive("Define OJUNNYSYS_TEST_CONNECTION para ejecutar pruebas MySQL aisladas.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseMySQL(connectionString!).Options;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenant = new Tenant("Empresa de prueba de caja", "cash-test", "cash-test@example.test", "America/Santo_Domingo");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, DisplayName = "Operador de caja",
            UserName = "cash-test@example.test", NormalizedUserName = "CASH-TEST@EXAMPLE.TEST",
            Email = "cash-test@example.test", NormalizedEmail = "CASH-TEST@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var cashAccount = new ChartAccount(tenant.Id, "1000", "Caja de prueba", AccountType.Asset);
        var incomeAccount = new ChartAccount(tenant.Id, "4100", "Ingresos varios", AccountType.Revenue);
        var paymentMethod = new TenantPaymentMethod(tenant.Id, "cash", "Efectivo", PaymentMethodType.Cash, ledgerAccountId: cashAccount.Id);
        var warehouse = new Warehouse(tenant.Id, "MAIN", "Almacén principal");
        var register = new PosRegister(tenant.Id, "MAIN", "Caja principal", warehouse.Id);
        var session = new CashSession(tenant.Id, "MAIN", 100m, user.Id);
        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenant.Id;
            db.Tenants.Add(tenant);
            db.Users.Add(user);
            db.ChartAccounts.AddRange(cashAccount, incomeAccount);
            db.TenantPaymentMethods.Add(paymentMethod);
            db.Warehouses.Add(warehouse);
            db.PosRegisters.Add(register);
            db.CashSessions.Add(session);
            await db.SaveChangesAsync();
        }

        var tenantContext = new CashTestTenantContext(tenant.Id, user.Id);
        var clock = new FixedBusinessClock(today);
        var factory = new TestContextFactory(options);
        var repository = new EfStoreRepository(factory, tenantContext, new TenantAccessGuard(factory, clock), clock,
            NullLogger<EfStoreRepository>.Instance);
        var request = new CashMovementRequest(CashMovementKind.CashIn, 25m, "Fondo para cambio", paymentMethod.Id,
            incomeAccount.Id, Guid.NewGuid());
        var first = await repository.RecordCashMovementAsync(session.Id, user.Id, request);
        var replay = await repository.RecordCashMovementAsync(session.Id, user.Id, request);
        Assert.AreEqual(125m, first.ExpectedCash);
        Assert.AreEqual(125m, replay.ExpectedCash);

        var closed = await repository.CloseCashSessionAsync(session.Id, user.Id,
            new CloseCashSessionRequest(128m, paymentMethod.Id, incomeAccount.Id));
        Assert.AreEqual(3m, closed.Difference);
        await using var verify = new StoreDbContext(options);
        verify.CurrentTenantId = tenant.Id;
        var movements = await verify.CashMovements.Where(x => x.SessionId == session.Id).ToListAsync();
        Assert.AreEqual(1, movements.Count, "El reintento con la misma clave no duplica el movimiento.");
        var movementEntry = await verify.JournalEntries.Include(x => x.Lines)
            .SingleAsync(x => x.SourceType == "CASHMOVEMENT" && x.SourceId == movements[0].Id);
        Assert.AreEqual(25m, movementEntry.Lines.Sum(x => x.Debit));
        Assert.AreEqual(25m, movementEntry.Lines.Sum(x => x.Credit));
        var closingEntry = await verify.JournalEntries.Include(x => x.Lines)
            .SingleAsync(x => x.SourceType == "CASHCLOSING" && x.SourceId == session.Id);
        Assert.AreEqual(3m, closingEntry.Lines.Sum(x => x.Debit));
        Assert.AreEqual(3m, closingEntry.Lines.Sum(x => x.Credit));
    }

    [TestMethod]
    public async Task ImportedStatement_MatchesLedgerMovement_AndClosesOnlyWhenBalancesAgree()
    {
        var connectionString = Environment.GetEnvironmentVariable("OJUNNYSYS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Inconclusive("Define OJUNNYSYS_TEST_CONNECTION para ejecutar pruebas MySQL aisladas.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseMySQL(connectionString!).Options;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenant = new Tenant("Empresa de prueba bancaria", "bank-test", "bank-test@example.test", "America/Santo_Domingo");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, DisplayName = "Operador de prueba",
            UserName = "bank-test@example.test", NormalizedUserName = "BANK-TEST@EXAMPLE.TEST",
            Email = "bank-test@example.test", NormalizedEmail = "BANK-TEST@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var bankLedger = new ChartAccount(tenant.Id, "1011", "Banco de prueba", AccountType.Asset);
        var revenueLedger = new ChartAccount(tenant.Id, "4100", "Ingresos de prueba", AccountType.Revenue);
        var bankMovement = new JournalEntryLine(tenant.Id, bankLedger.Id, bankLedger.Code, bankLedger.Name, 100m, 0m, "Depósito estado");
        var offsetMovement = new JournalEntryLine(tenant.Id, revenueLedger.Id, revenueLedger.Code, revenueLedger.Name, 0m, 100m, "Contrapartida");
        var entry = new JournalEntry(tenant.Id, "IT-BANK-001", today, "Depósito para prueba de conciliación", user.Id,
            [bankMovement, offsetMovement]);

        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenant.Id;
            db.Tenants.Add(tenant);
            db.Users.Add(user);
            db.ChartAccounts.AddRange(bankLedger, revenueLedger);
            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var tenantContext = new TestTenantContext(tenant.Id);
        var clock = new FixedBusinessClock(today);
        var repository = new EfBankingRepository(new TestContextFactory(options), tenantContext, clock);
        var service = new BankingService(repository, clock);
        var account = await service.CreateBankAccountAsync(new CreateBankAccountRequest(
            "Cuenta de prueba", "Banco de pruebas", "0123", "DOP", bankLedger.Id));
        Assert.AreEqual("0123", account.AccountSuffix);
        Assert.AreEqual(100m, account.BookBalance);

        var reconciliation = await service.CreateReconciliationAsync(new CreateBankReconciliationRequest(
            account.Id, "ESTADO-IT-001", today, today, 0m, 100m,
            [new BankStatementLineRequest(today, "Depósito estado", 100m, "TR-IT-001")]));
        Assert.AreEqual(1, reconciliation.UnmatchedBookMovements.Count);
        Assert.AreEqual(-100m, reconciliation.Difference);

        var didNotCloseWithMissingMatch = false;
        try { await service.CompleteReconciliationAsync(reconciliation.Id); }
        catch (DomainException) { didNotCloseWithMissingMatch = true; }
        Assert.IsTrue(didNotCloseWithMissingMatch, "No debe cerrar con una diferencia ni una línea sin conciliar.");

        reconciliation = await service.MatchStatementLineAsync(reconciliation.Id, reconciliation.Lines.Single().Id, bankMovement.Id);
        Assert.AreEqual(0m, reconciliation.Difference);
        Assert.AreEqual(1, reconciliation.MatchedLineCount);
        Assert.AreEqual(0, reconciliation.UnmatchedBookMovements.Count);
        reconciliation = await service.CompleteReconciliationAsync(reconciliation.Id);
        Assert.AreEqual(BankReconciliationStatus.Reconciled, reconciliation.Status);

        tenantContext.SetTenantId(Guid.NewGuid());
        Assert.AreEqual(0, (await service.GetBankAccountsAsync()).Count, "Una empresa distinta no debe ver esta cuenta bancaria.");
    }

    private sealed class TestContextFactory(DbContextOptions<StoreDbContext> options) : IDbContextFactory<StoreDbContext>
    {
        public StoreDbContext CreateDbContext() => new(options);
        public Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class TestTenantContext(Guid? tenantId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;
        public Guid? CurrentUserId => null;
        public Guid GetRequiredTenantId() => CurrentTenantId ?? throw new TenantAccessException("Test tenant missing.");
        public void SetTenantId(Guid? tenantId) => CurrentTenantId = tenantId;
    }

    private sealed class CashTestTenantContext(Guid tenantId, Guid userId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;
        public Guid? CurrentUserId => userId;
        public Guid GetRequiredTenantId() => CurrentTenantId ?? throw new TenantAccessException("Test tenant missing.");
        public void SetTenantId(Guid? value) => CurrentTenantId = value;
    }

    private sealed class FixedBusinessClock(DateOnly date) : IBusinessClock
    {
        public DateTimeOffset UtcNow => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        public DateTimeOffset ToBusinessTime(DateTimeOffset instantUtc) => instantUtc;
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) CurrentBusinessDayUtc => GetUtcRange(date, date);
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly startInclusive, DateOnly endInclusive) =>
            (new DateTimeOffset(startInclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(endInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }
}


