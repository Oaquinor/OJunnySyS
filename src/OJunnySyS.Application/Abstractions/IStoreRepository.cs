using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;

namespace OJunnySyS.Application.Abstractions;

public interface IStoreRepository
{
    Task<IReadOnlyList<Product>> SearchProductsAsync(string? query, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<InventoryValuationDto> GetInventoryValuationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductStockSnapshot>> SearchProductsInWarehouseAsync(string? query, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? query, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetCustomerSalesAsync(Guid customerId, int count = 100, CancellationToken cancellationToken = default);
    Task<CustomerStatementData> GetCustomerStatementDataAsync(Guid customerId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetRecentSalesAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetSalesInPeriodAsync(DateTimeOffset from, DateTimeOffset to, Guid? warehouseId = null, Guid? sellerUserId = null, string? documentNumber = null, CancellationToken cancellationToken = default);
    Task<Customer> CreateCustomerAsync(string firstName, string lastName, string email, string phone, string? company, CancellationToken cancellationToken = default);
    Task<Customer> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Customer> DeactivateCustomerAsync(Guid customerId, long expectedRevision, CancellationToken cancellationToken = default);
    Task<Product> CreateProductAsync(string sku, string name, string category, decimal unitPrice, int stock, string? barcode, decimal? taxRateOverride = null, decimal? initialUnitCost = null, Guid? taxProfileId = null, string? imageUrl = null, Guid? initialWarehouseId = null, CancellationToken cancellationToken = default);
    Task<Product> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Product> SetProductActiveAsync(Guid productId, SetProductActiveRequest request, CancellationToken cancellationToken = default);
    Task<InventoryTransferResult> TransferInventoryAsync(InventoryTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryAdjustmentDto>> GetInventoryAdjustmentsAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<InventoryAdjustmentDto> AdjustInventoryAsync(InventoryAdjustmentRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Sale> CheckoutAsync(CheckoutRequest request, Guid cashierUserId, CancellationToken cancellationToken = default);
    Task<CashSessionDto?> GetCurrentCashSessionAsync(string registerCode = "MAIN", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashSessionDto>> GetRecentCashSessionsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashSessionDto>> GetCashSessionsInPeriodAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<CashSessionDto> OpenCashSessionAsync(Guid userId, OpenCashSessionRequest request, CancellationToken cancellationToken = default);
    Task<CashSessionDto> RecordCashMovementAsync(Guid sessionId, Guid userId, CashMovementRequest request, CancellationToken cancellationToken = default);
    Task<CashSessionDto> CloseCashSessionAsync(Guid sessionId, Guid userId, CloseCashSessionRequest request, CancellationToken cancellationToken = default);
    Task<SaleReceiptDto?> GetSaleReceiptAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> SearchSuppliersAsync(string? query, CancellationToken cancellationToken = default);
    Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Supplier> UpdateSupplierAsync(Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Supplier> DeactivateSupplierAsync(Guid supplierId, long expectedRevision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default);
    Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrder> ReceivePurchaseOrderAsync(Guid purchaseOrderId, Guid receivedByUserId, ReceivePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrder> RecordSupplierPaymentAsync(Guid purchaseOrderId, Guid paidByUserId, CreateSupplierPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrder> CancelPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayableBalanceDto>> GetOpenPayablesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetOpenReceivablesAsync(CancellationToken cancellationToken = default);
    Task<Sale> RecordCustomerReceiptAsync(Guid saleId, Guid actorId, CustomerReceiptRequest request, CancellationToken cancellationToken = default);
    Task<Sale> ReturnSaleAsync(Guid saleId, Guid actorId, CreateSaleReturnRequest request, CancellationToken cancellationToken = default);
}
