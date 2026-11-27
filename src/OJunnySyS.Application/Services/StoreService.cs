using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;

namespace OJunnySyS.Application.Services;

public sealed class StoreService(IStoreRepository repository, IBusinessClock clock)
{
    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string? query, bool includeInactive = false, CancellationToken ct = default) =>
        (await repository.SearchProductsAsync(query, includeInactive, ct)).Select(Map).ToArray();

    public async Task<IReadOnlyList<ProductDto>> SearchProductsInWarehouseAsync(string? query, Guid warehouseId, CancellationToken ct = default) =>
        (await repository.SearchProductsInWarehouseAsync(query, warehouseId, ct))
            .Select(x => Map(x.Product) with { StockOnHand = x.WarehouseStock }).ToArray();

    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string? query, CancellationToken ct = default) =>
        (await repository.SearchCustomersAsync(query, ct)).Select(Map).ToArray();

    public async Task<CustomerDto?> GetCustomerAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await repository.GetCustomerAsync(id, ct);
        return customer is null ? null : Map(customer);
    }

    public async Task<IReadOnlyList<SaleDto>> GetCustomerSalesAsync(Guid customerId, int count = 100, CancellationToken ct = default) =>
        (await repository.GetCustomerSalesAsync(customerId, count, ct)).Select(Map).ToArray();

    public async Task<IReadOnlyList<SaleDto>> GetRecentSalesAsync(int count, CancellationToken ct = default) =>
        (await repository.GetRecentSalesAsync(count, ct)).Select(Map).ToArray();

    public async Task<SalesReport> GetSalesReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (from > to) throw new OJunnySyS.Domain.Common.DomainException("La fecha inicial debe ser anterior o igual a la fecha final.");
        if (to.DayNumber - from.DayNumber > 365) throw new OJunnySyS.Domain.Common.DomainException("El período máximo del informe es de 366 días.");
        var (startUtc, endUtc) = clock.GetUtcRange(from, to);
        var sales = await repository.GetSalesInPeriodAsync(startUtc, endUtc, ct);
        var completed = sales.Where(x => x.Status == SaleStatus.Completed).ToArray();
        var payments = completed.GroupBy(x => x.PaymentMethod)
            .Select(group => new PaymentBreakdown(group.Key, group.Count(), group.Sum(x => x.Total)))
            .OrderByDescending(x => x.Total).ToArray();
        var topProducts = completed.SelectMany(x => x.Lines)
            .GroupBy(x => x.ProductName)
            .Select(group => new ProductSalesSummary(group.Key, group.Sum(x => x.Quantity), group.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.Revenue).Take(5).ToArray();
        var dailySales = completed.GroupBy(x => DateOnly.FromDateTime(clock.ToBusinessTime(x.CreatedAtUtc).DateTime))
            .Select(group => new DailySalesSummary(group.Key, group.Sum(x => x.Total)))
            .OrderBy(x => x.Date).ToArray();
        return new SalesReport(from, to, completed.Length, completed.Sum(x => x.Subtotal), completed.Sum(x => x.TaxAmount),
            completed.Sum(x => x.Total), payments, topProducts, dailySales);
    }

    public async Task<CustomerDto> CreateCustomerAsync(string firstName, string lastName, string email, string phone, string? company, CancellationToken ct = default)
    {
        var customer = await repository.CreateCustomerAsync(firstName, lastName, email, phone, company, ct);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default) =>
        Map(await repository.UpdateCustomerAsync(id, request, ct));

    public async Task<CustomerDto> DeactivateCustomerAsync(Guid id, long expectedRevision, CancellationToken ct = default) =>
        Map(await repository.DeactivateCustomerAsync(id, expectedRevision, ct));

    public async Task<ProductDto> CreateProductAsync(string sku, string name, string category, decimal unitPrice, int stock, string? barcode, CancellationToken ct = default)
    {
        var product = await repository.CreateProductAsync(sku, name, category, unitPrice, stock, barcode, ct);
        return Map(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default) =>
        Map(await repository.UpdateProductAsync(id, request, ct));

    public async Task<ProductDto> SetProductActiveAsync(Guid id, SetProductActiveRequest request, CancellationToken ct = default) =>
        Map(await repository.SetProductActiveAsync(id, request, ct));

    public Task<InventoryTransferResult> TransferInventoryAsync(InventoryTransferRequest request, CancellationToken ct = default) =>
        repository.TransferInventoryAsync(request, ct);

    public async Task<SaleDto> CheckoutAsync(CheckoutRequest request, Guid cashierUserId, CancellationToken ct = default)
    {
        var sale = await repository.CheckoutAsync(request, cashierUserId, ct);
        return Map(sale);
    }

    public Task<CashSessionDto?> GetCurrentCashSessionAsync(string registerCode = "MAIN", CancellationToken ct = default) => repository.GetCurrentCashSessionAsync(registerCode, ct);
    public Task<IReadOnlyList<CashSessionDto>> GetRecentCashSessionsAsync(int count = 10, CancellationToken ct = default) => repository.GetRecentCashSessionsAsync(count, ct);
    public async Task<CashSessionReport> GetCashSessionReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (from > to || to.DayNumber - from.DayNumber > 365) throw new OJunnySyS.Domain.Common.DomainException("El período del informe debe ser válido y no superar 366 días.");
        var (startUtc, endUtc) = clock.GetUtcRange(from, to);
        var sessions = await repository.GetCashSessionsInPeriodAsync(startUtc, endUtc, ct);
        return new CashSessionReport(from, to, sessions.Count, sessions.Count(x => x.Status == CashSessionStatus.Open),
            sessions.Sum(x => x.CashSales), sessions.Sum(x => x.CashIn), sessions.Sum(x => x.CashOut),
            sessions.Where(x => x.CountedCash.HasValue).Sum(x => x.Difference ?? 0m), sessions);
    }
    public Task<CashSessionDto> OpenCashSessionAsync(Guid userId, OpenCashSessionRequest request, CancellationToken ct = default) => repository.OpenCashSessionAsync(userId, request, ct);
    public Task<CashSessionDto> RecordCashMovementAsync(Guid sessionId, Guid userId, CashMovementRequest request, CancellationToken ct = default) => repository.RecordCashMovementAsync(sessionId, userId, request, ct);
    public Task<CashSessionDto> CloseCashSessionAsync(Guid sessionId, Guid userId, decimal countedCash, CancellationToken ct = default) => repository.CloseCashSessionAsync(sessionId, userId, countedCash, ct);
    public Task<SaleReceiptDto?> GetSaleReceiptAsync(Guid saleId, CancellationToken ct = default) => repository.GetSaleReceiptAsync(saleId, ct);

    public async Task<IReadOnlyList<SupplierDto>> SearchSuppliersAsync(string? query, CancellationToken ct = default) =>
        (await repository.SearchSuppliersAsync(query, ct)).Select(Map).ToArray();

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await repository.CreateSupplierAsync(request, ct);
        return Map(supplier);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default) => Map(await repository.UpdateSupplierAsync(id, request, ct));
    public async Task<SupplierDto> DeactivateSupplierAsync(Guid id, long expectedRevision, CancellationToken ct = default) => Map(await repository.DeactivateSupplierAsync(id, expectedRevision, ct));

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken ct = default) =>
        (await repository.GetPurchaseOrdersAsync(ct)).Select(Map).ToArray();

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default) =>
        Map(await repository.CreatePurchaseOrderAsync(request, ct));

    public async Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(Guid id, IReadOnlyCollection<ReceivePurchaseLineRequest> lines, CancellationToken ct = default) =>
        Map(await repository.ReceivePurchaseOrderAsync(id, lines, ct));

    public async Task<PurchaseOrderDto> RecordSupplierPaymentAsync(Guid id, Guid actorId, CreateSupplierPaymentRequest request, CancellationToken ct = default) =>
        Map(await repository.RecordSupplierPaymentAsync(id, actorId, request, ct));

    public async Task<PurchaseOrderDto> CancelPurchaseOrderAsync(Guid id, CancellationToken ct = default) => Map(await repository.CancelPurchaseOrderAsync(id, ct));

    public async Task<PayablesAgingReport> GetPayablesAgingAsync(DateOnly? asOfDate = null, CancellationToken ct = default)
    {
        var asOf = asOfDate ?? DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);
        var balances = await repository.GetOpenPayablesAsync(ct);
        var items = balances.Where(x => x.BalanceDue > 0).Select(item =>
        {
            var daysPastDue = item.DueDate is { } due ? Math.Max(0, asOf.DayNumber - due.DayNumber) : 0;
            var bucket = item.DueDate is null ? PayableAgingBucket.NoDueDate
                : daysPastDue == 0 ? PayableAgingBucket.Current
                : daysPastDue <= 30 ? PayableAgingBucket.Days1To30
                : daysPastDue <= 60 ? PayableAgingBucket.Days31To60
                : daysPastDue <= 90 ? PayableAgingBucket.Days61To90
                : PayableAgingBucket.Over90Days;
            return new PayableAgingLineDto(item.PurchaseOrderId, item.Number, item.SupplierName, item.Reference, item.DueDate,
                item.Total, item.AmountPaid, item.BalanceDue, daysPastDue, bucket);
        }).ToArray();
        var bands = Enum.GetValues<PayableAgingBucket>().Select(bucket =>
        {
            var matching = items.Where(x => x.Bucket == bucket).ToArray();
            return new PayableAgingBand(bucket, matching.Length, matching.Sum(x => x.BalanceDue));
        }).ToArray();
        return new PayablesAgingReport(asOf, items.Length, items.Sum(x => x.BalanceDue), bands,
            items.OrderByDescending(x => x.DaysPastDue).ThenBy(x => x.DueDate).Take(500).ToArray());
    }

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default)
    {
        var (startUtc, endUtc) = clock.CurrentBusinessDayUtc;
        var sales = await repository.GetSalesInPeriodAsync(startUtc, endUtc, ct);
        var products = await repository.SearchProductsAsync(null, cancellationToken: ct);
        var customers = await repository.SearchCustomersAsync(null, ct);
        return new DashboardSummary(sales.Where(x => x.Status == SaleStatus.Completed).Sum(x => x.Total), sales.Count(x => x.Status == SaleStatus.Completed), customers.Count, products.Count(x => x.StockOnHand <= 5), (await repository.GetRecentSalesAsync(6, ct)).Select(Map).ToArray());
    }

    private static ProductDto Map(Product product) => new(product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.StockOnHand, product.Barcode, product.IsActive, product.Revision);
    private static CustomerDto Map(Customer customer) => new(customer.Id, customer.FullName, customer.Email, customer.Phone, customer.Company, customer.LifetimeValue, customer.Revision, customer.FirstName, customer.LastName, customer.CreatedAtUtc);
    private static SaleDto Map(Sale sale) => new(sale.Id, sale.Number, sale.CustomerName, sale.Subtotal, sale.TaxAmount, sale.Total, sale.PaymentMethod, sale.CreatedAtUtc, sale.Status);
    private static SupplierDto Map(Supplier supplier) => new(supplier.Id, supplier.Name, supplier.TaxIdentifier, supplier.Email, supplier.Phone, supplier.Notes, supplier.Revision);
    private static PurchaseOrderDto Map(PurchaseOrder order) => new(order.Id, order.Number, order.SupplierName, order.Reference, order.DueDate, order.Status, order.Total, order.AmountPaid, order.BalanceDue, order.CreatedAtUtc,
        order.Lines.Select(x => new PurchaseLineDto(x.Id, x.ProductId, x.Sku, x.ProductName, x.Quantity, x.ReceivedQuantity, x.UnitCost, x.LineTotal)).ToArray(),
        order.Payments.OrderByDescending(x => x.CreatedAtUtc).Select(x => new SupplierPaymentDto(x.Id, x.IdempotencyKey, x.Amount, x.Method, x.Reference, x.PaidByUserId, x.CreatedAtUtc)).ToArray(), order.WarehouseId);
}
