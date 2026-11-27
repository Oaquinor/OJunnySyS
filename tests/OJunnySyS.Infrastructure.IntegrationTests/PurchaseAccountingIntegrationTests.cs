using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Infrastructure.Identity;
using OJunnySyS.Infrastructure.Persistence;

namespace OJunnySyS.Infrastructure.IntegrationTests;

[TestClass]
public sealed class PurchaseAccountingIntegrationTests
{
    [TestMethod]
    public async Task ProductTaxProfile_IsAppliedByPosAndHistoricalSaleKeepsRateSnapshot()
    {
        var connectionString = Environment.GetEnvironmentVariable("OJUNNYSYS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Inconclusive("Define OJUNNYSYS_TEST_CONNECTION para ejecutar pruebas MySQL aisladas.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseMySQL(connectionString!).Options;
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenant = new Tenant("Empresa POS impuestos", "tax-pos-it", "tax-pos-it@example.test", "America/Santo_Domingo");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, DisplayName = "Cajero de prueba",
            UserName = "tax-pos-it@example.test", NormalizedUserName = "TAX-POS-IT@EXAMPLE.TEST",
            Email = "tax-pos-it@example.test", NormalizedEmail = "TAX-POS-IT@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var profile = new TaxProfile(tenant.Id, "TAX-REDUCED", "Tasa reducida", .10m);
        var warehouse = new Warehouse(tenant.Id, "MAIN", "Almacén principal");
        var product = new Product(tenant.Id, "TAX-ITEM", "Artículo gravado", "Pruebas", 100m, 10, initialUnitCost: 20m, taxProfileId: profile.Id);
        var register = new PosRegister(tenant.Id, "MAIN", "Caja principal", warehouse.Id);
        var cashSession = new CashSession(tenant.Id, register.Code, 500m, user.Id);
        var accounts = new[]
        {
            new ChartAccount(tenant.Id, "1000", "Caja", AccountType.Asset),
            new ChartAccount(tenant.Id, "1200", "Inventario", AccountType.Asset),
            new ChartAccount(tenant.Id, "2100", "Impuesto por pagar", AccountType.Liability),
            new ChartAccount(tenant.Id, "4000", "Ventas", AccountType.Revenue),
            new ChartAccount(tenant.Id, "5000", "Costo de ventas", AccountType.Expense)
        };
        var cash = new TenantPaymentMethod(tenant.Id, "cash", "Efectivo", PaymentMethodType.Cash, ledgerAccountId: accounts[0].Id);
        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenant.Id;
            db.Tenants.Add(tenant); db.Users.Add(user); db.TaxProfiles.Add(profile); db.Warehouses.Add(warehouse);
            db.Products.Add(product); db.PosRegisters.Add(register); db.CashSessions.Add(cashSession);
            db.WarehouseStocks.Add(new WarehouseStock(tenant.Id, warehouse.Id, product.Id, 10));
            db.ChartAccounts.AddRange(accounts); db.TenantPaymentMethods.Add(cash);
            await db.SaveChangesAsync();
        }

        var clock = new FixedClock(date);
        var tenantContext = new PurchaseTestTenantContext(tenant.Id, user.Id);
        var factory = new PurchaseTestContextFactory(options);
        var repository = new EfStoreRepository(factory, tenantContext, new TenantAccessGuard(factory, clock), clock,
            NullLogger<EfStoreRepository>.Instance);
        var first = await repository.CheckoutAsync(new CheckoutRequest(null, PaymentMethod.Cash,
            [new CheckoutLine(product.Id, 1)], Guid.NewGuid(), register.Code), user.Id);
        Assert.AreEqual(10m, first.TaxAmount);
        Assert.AreEqual(110m, first.Total);
        Assert.AreEqual(.10m, first.Lines.Single().TaxRate);

        var profileRepository = new EfTaxProfileRepository(factory, tenantContext, new TenantAccessGuard(factory, clock));
        await profileRepository.UpdateAsync(profile.Id, new UpdateTaxProfileRequest("Tasa reducida actualizada", .05m, profile.Revision));
        var second = await repository.CheckoutAsync(new CheckoutRequest(null, PaymentMethod.Cash,
            [new CheckoutLine(product.Id, 1)], Guid.NewGuid(), register.Code), user.Id);
        Assert.AreEqual(5m, second.TaxAmount);
        await using var verify = new StoreDbContext(options);
        verify.CurrentTenantId = tenant.Id;
        var historicalSale = await verify.Sales.Include(x => x.Lines).SingleAsync(x => x.Id == first.Id);
        Assert.AreEqual(.10m, historicalSale.Lines.Single().TaxRate, "Cambiar el catálogo no modifica el impuesto de ventas pasadas.");
        Assert.AreEqual(8, (await verify.WarehouseStocks.SingleAsync(x => x.WarehouseId == warehouse.Id && x.ProductId == product.Id)).Quantity);
    }

    [TestMethod]
    public async Task PartialReceiptsAndSupplierPayments_PostBalancedEntriesAndUpdateInventoryAndPayables()
    {
        var connectionString = Environment.GetEnvironmentVariable("OJUNNYSYS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Inconclusive("Define OJUNNYSYS_TEST_CONNECTION para ejecutar pruebas MySQL aisladas.");
        var options = new DbContextOptionsBuilder<StoreDbContext>().UseMySQL(connectionString!).Options;
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenant = new Tenant("Empresa de integración de compras", "purchase-it", "purchase-it@example.test", "America/Santo_Domingo");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, DisplayName = "Comprador de prueba",
            UserName = "purchase-it@example.test", NormalizedUserName = "PURCHASE-IT@EXAMPLE.TEST",
            Email = "purchase-it@example.test", NormalizedEmail = "PURCHASE-IT@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var supplier = new Supplier(tenant.Id, "Proveedor de integración", "TEST-123", "supplier@example.test", "8095550100");
        var product = new Product(tenant.Id, "IT-ITEM", "Artículo de prueba", "Pruebas", 100m, 0, initialUnitCost: 0m);
        var warehouse = new Warehouse(tenant.Id, "MAIN", "Almacén principal");
        var accounts = new[]
        {
            new ChartAccount(tenant.Id, "1000", "Caja", AccountType.Asset),
            new ChartAccount(tenant.Id, "1010", "Banco", AccountType.Asset),
            new ChartAccount(tenant.Id, "1200", "Inventario", AccountType.Asset),
            new ChartAccount(tenant.Id, "1300", "Impuesto acreditable", AccountType.Asset),
            new ChartAccount(tenant.Id, "2000", "Cuentas por pagar", AccountType.Liability)
        };
        var stock = new WarehouseStock(tenant.Id, warehouse.Id, product.Id, 0);
        await using (var db = new StoreDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.CurrentTenantId = tenant.Id;
            db.Tenants.Add(tenant);
            db.Users.Add(user);
            db.Suppliers.Add(supplier);
            db.Products.Add(product);
            db.Warehouses.Add(warehouse);
            db.ChartAccounts.AddRange(accounts);
            db.WarehouseStocks.Add(stock);
            await db.SaveChangesAsync();
        }

        var clock = new FixedClock(date);
        var context = new PurchaseTestTenantContext(tenant.Id, user.Id);
        var factory = new PurchaseTestContextFactory(options);
        var repository = new EfStoreRepository(factory, context, new TenantAccessGuard(factory, clock), clock,
            NullLogger<EfStoreRepository>.Instance);
        var order = await repository.CreatePurchaseOrderAsync(new CreatePurchaseOrderRequest(supplier.Id, "PO-IT-001",
            [new CreatePurchaseLineRequest(product.Id, 10, 50m, 0.18m)], date.AddDays(30), warehouse.Id));
        Assert.AreEqual(590m, order.Total);
        Assert.AreEqual(590m, order.BalanceDue);

        var firstReceipt = new ReceivePurchaseOrderRequest(Guid.NewGuid(), [new ReceivePurchaseLineRequest(order.Lines.Single().Id, 4)]);
        order = await repository.ReceivePurchaseOrderAsync(order.Id, user.Id, firstReceipt);
        Assert.AreEqual(PurchaseStatus.PartiallyReceived, order.Status);
        Assert.AreEqual(4, order.Lines.Single().ReceivedQuantity);
        order = await repository.ReceivePurchaseOrderAsync(order.Id, user.Id, firstReceipt);
        Assert.AreEqual(4, order.Lines.Single().ReceivedQuantity, "El reintento de la misma recepción no repite el inventario.");
        var rejectedChangedReplay = false;
        try
        {
            await repository.ReceivePurchaseOrderAsync(order.Id, user.Id,
                new ReceivePurchaseOrderRequest(firstReceipt.IdempotencyKey, [new ReceivePurchaseLineRequest(order.Lines.Single().Id, 5)]));
        }
        catch (DomainException) { rejectedChangedReplay = true; }
        Assert.IsTrue(rejectedChangedReplay, "La misma clave no puede representar cantidades diferentes.");
        order = await repository.ReceivePurchaseOrderAsync(order.Id, user.Id,
            new ReceivePurchaseOrderRequest(Guid.NewGuid(), [new ReceivePurchaseLineRequest(order.Lines.Single().Id, 6)]));
        Assert.AreEqual(PurchaseStatus.Received, order.Status);
        Assert.AreEqual(590m, order.BalanceDue, "La recepción genera la cuenta por pagar; el saldo solo baja con pagos.");

        var paymentKey = Guid.NewGuid();
        var paymentRequest = new CreateSupplierPaymentRequest(paymentKey, 100m, SupplierPaymentMethod.BankTransfer, "TR-IT-001");
        order = await repository.RecordSupplierPaymentAsync(order.Id, user.Id, paymentRequest);
        Assert.AreEqual(490m, order.BalanceDue);
        var replay = await repository.RecordSupplierPaymentAsync(order.Id, user.Id, paymentRequest);
        Assert.AreEqual(490m, replay.BalanceDue);
        order = await repository.RecordSupplierPaymentAsync(order.Id, user.Id,
            new CreateSupplierPaymentRequest(Guid.NewGuid(), 490m, SupplierPaymentMethod.BankTransfer, "TR-IT-002"));
        Assert.AreEqual(0m, order.BalanceDue);

        await using var verify = new StoreDbContext(options);
        verify.CurrentTenantId = tenant.Id;
        var persistedProduct = await verify.Products.SingleAsync(x => x.Id == product.Id);
        Assert.AreEqual(10, persistedProduct.StockOnHand);
        Assert.AreEqual(50m, persistedProduct.UnitCost);
        Assert.AreEqual(10, (await verify.WarehouseStocks.SingleAsync(x => x.WarehouseId == warehouse.Id && x.ProductId == product.Id)).Quantity);
        var receipts = await verify.JournalEntries.Include(x => x.Lines).Where(x => x.SourceType == "PURCHASERECEIPT").ToListAsync();
        Assert.AreEqual(2, receipts.Count, "Cada recepción parcial tiene su asiento.");
        Assert.AreEqual(2, await verify.PurchaseReceiptBatches.CountAsync(x => x.PurchaseOrderId == order.Id));
        Assert.AreEqual(2, await verify.InventoryMovements.CountAsync(x => x.Reference == order.Number));
        Assert.AreEqual(590m, receipts.Sum(x => x.Lines.Sum(line => line.Debit)));
        Assert.AreEqual(590m, receipts.Sum(x => x.Lines.Sum(line => line.Credit)));
        Assert.IsTrue(receipts.All(x => x.Lines.Sum(line => line.Debit) == x.Lines.Sum(line => line.Credit)));
        var payments = await verify.SupplierPayments.CountAsync(x => x.PurchaseOrderId == order.Id);
        Assert.AreEqual(2, payments, "El reintento no duplica el abono al proveedor.");
        var paymentEntries = await verify.JournalEntries.Include(x => x.Lines).Where(x => x.SourceType == "SUPPLIERPAYMENT").ToListAsync();
        Assert.AreEqual(2, paymentEntries.Count);
        Assert.AreEqual(590m, paymentEntries.Sum(x => x.Lines.Sum(line => line.Debit)));
        Assert.AreEqual(590m, paymentEntries.Sum(x => x.Lines.Sum(line => line.Credit)));
    }

    private sealed class PurchaseTestContextFactory(DbContextOptions<StoreDbContext> options) : IDbContextFactory<StoreDbContext>
    {
        public StoreDbContext CreateDbContext() => new(options);
        public Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class PurchaseTestTenantContext(Guid tenantId, Guid userId) : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; } = tenantId;
        public Guid? CurrentUserId => userId;
        public Guid GetRequiredTenantId() => CurrentTenantId ?? throw new TenantAccessException("Test tenant missing.");
        public void SetTenantId(Guid? value) => CurrentTenantId = value;
    }

    private sealed class FixedClock(DateOnly date) : IBusinessClock
    {
        public DateTimeOffset UtcNow => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        public DateTimeOffset ToBusinessTime(DateTimeOffset instantUtc) => instantUtc;
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) CurrentBusinessDayUtc => GetUtcRange(date, date);
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly startInclusive, DateOnly endInclusive) =>
            (new DateTimeOffset(startInclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(endInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }
}
