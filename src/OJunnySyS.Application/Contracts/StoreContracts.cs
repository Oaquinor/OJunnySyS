using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Crm;

namespace OJunnySyS.Application.Contracts;

public sealed record ProductDto(Guid Id, string Sku, string Name, string Category, decimal UnitPrice, int StockOnHand, string? Barcode, bool IsActive = true, long Revision = 0, decimal? TaxRateOverride = null, Guid? TaxProfileId = null, string? TaxProfileName = null, decimal? TaxProfileRate = null, string? ImageUrl = null);
public sealed record UpdateProductRequest(string Sku, string Name, string Category, decimal UnitPrice, string? Barcode, long ExpectedRevision, decimal? TaxRateOverride = null, Guid? TaxProfileId = null, string? ImageUrl = null);
public sealed record TaxProfileDto(Guid Id, string Code, string Name, decimal Rate, bool IsActive, long Revision);
public sealed record CreateTaxProfileRequest(string Code, string Name, decimal Rate);
public sealed record UpdateTaxProfileRequest(string Name, decimal Rate, long ExpectedRevision);
public sealed record SetTaxProfileActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record SetProductActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record CustomerDto(Guid Id, string Name, string Email, string Phone, string? Company, decimal LifetimeValue, long Revision, string FirstName, string LastName, DateTimeOffset CreatedAtUtc = default);
public sealed record UpdateCustomerRequest(string FirstName, string LastName, string Email, string Phone, string? Company, long ExpectedRevision);
public sealed record DeactivateCustomerRequest(long ExpectedRevision);
public sealed record SaleDto(Guid Id, string Number, string CustomerName, decimal Subtotal, decimal TaxAmount, decimal Total, PaymentMethod PaymentMethod, DateTimeOffset CreatedAtUtc, SaleStatus Status = SaleStatus.Completed, string? PaymentSummary = null, string? SellerName = null, DateOnly? DueDate = null, decimal AmountPaid = 0m, decimal BalanceDue = 0m);
public sealed record SalesQuoteLineRequest(Guid ProductId, int Quantity, decimal? UnitPrice = null);
public sealed record CreateSalesQuoteRequest(Guid CustomerId, DateOnly ValidUntil, string? Notes, IReadOnlyCollection<SalesQuoteLineRequest> Lines);
public sealed record UpdateSalesQuoteRequest(Guid CustomerId, DateOnly ValidUntil, string? Notes, long ExpectedRevision, IReadOnlyCollection<SalesQuoteLineRequest> Lines);
public sealed record SalesQuoteStatusRequest(long ExpectedRevision);
public sealed record ConvertSalesQuoteRequest(long ExpectedRevision, string RegisterCode, DateOnly DueDate);
public sealed record SalesQuoteLineDto(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, decimal TaxRate, decimal TaxAmount);
public sealed record SalesQuoteDto(Guid Id, string Number, Guid CustomerId, string CustomerName, DateOnly ValidUntil, string? Notes, SalesQuoteStatus Status, decimal Subtotal, decimal TaxAmount, decimal Total, DateTimeOffset CreatedAtUtc, long Revision, Guid? ConvertedSaleId, IReadOnlyList<SalesQuoteLineDto> Lines);
public sealed record CheckoutLine(Guid ProductId, int Quantity, decimal DiscountPercent = 0m);
public sealed record CheckoutPaymentLine(Guid PaymentMethodId, decimal Amount, string? Reference = null);
public sealed record CheckoutRequest(Guid? CustomerId, PaymentMethod PaymentMethod, IReadOnlyCollection<CheckoutLine> Lines, Guid IdempotencyKey = default, string RegisterCode = "MAIN", IReadOnlyCollection<CheckoutPaymentLine>? Payments = null, Guid? SellerUserId = null, DateOnly? DueDate = null, Guid? SalesQuoteId = null, long? SalesQuoteRevision = null, Guid? ParkedSaleId = null);
public sealed record CustomerReceiptRequest(Guid IdempotencyKey, decimal Amount, Guid PaymentMethodId, string? Reference = null, string RegisterCode = "MAIN");
public sealed record SaleReturnLineRequest(Guid SaleLineId, int Quantity);
public sealed record CreateSaleReturnRequest(Guid IdempotencyKey, IReadOnlyCollection<SaleReturnLineRequest> Lines, Guid? RefundPaymentMethodId = null, string RegisterCode = "MAIN");
public sealed record ReceivableDto(Guid SaleId, string Number, Guid CustomerId, string CustomerName, DateOnly? DueDate, DateTimeOffset IssuedAtUtc, decimal Total, decimal AmountPaid, decimal BalanceDue);
public sealed record ReceivableAgingBucketDto(string Bucket, int Count, decimal Balance);
public sealed record ReceivablesAgingReport(DateOnly AsOfDate, int OpenInvoices, decimal TotalBalance, IReadOnlyList<ReceivableAgingBucketDto> Bands, IReadOnlyList<ReceivableDto> Items);
public enum CustomerStatementEntryType { Sale, Payment, CreditNote }
public sealed record CustomerStatementMovement(Guid SaleId, DateTimeOffset OccurredAtUtc, string Reference, CustomerStatementEntryType Type, string Description, DateOnly? DueDate, decimal Debit, decimal Credit);
public sealed record CustomerStatementData(decimal OpeningBalance, IReadOnlyList<CustomerStatementMovement> Movements);
public sealed record CustomerStatementLine(Guid SaleId, DateTimeOffset OccurredAtUtc, string Reference, CustomerStatementEntryType Type, string Description, DateOnly? DueDate, decimal Debit, decimal Credit, decimal Balance);
public sealed record CustomerStatementReport(Guid CustomerId, string CustomerName, DateOnly From, DateOnly To, decimal OpeningBalance, decimal Invoiced, decimal Collected, decimal ClosingBalance, IReadOnlyList<CustomerStatementLine> Entries);
public sealed record DashboardSummary(decimal SalesToday, int OrdersToday, int ActiveCustomers, int LowStockProducts, IReadOnlyList<SaleDto> RecentSales);
public sealed record PaymentBreakdown(string Method, int Sales, decimal Total);
public sealed record ProductSalesSummary(string ProductName, int Quantity, decimal Revenue, decimal Cost, decimal GrossProfit, int UncostedUnits);
public sealed record InventoryValuationDto(decimal CostedInventoryValue, int UncostedUnits);
public sealed record SellerSalesSummary(string SellerName, int Sales, decimal Revenue);
public sealed record DailySalesSummary(DateOnly Date, decimal Total);
public sealed record CustomerSalesSummary(Guid? CustomerId, string CustomerName, int Sales, decimal Subtotal, decimal TaxAmount, decimal Total);
public sealed record WarehouseSalesSummary(Guid WarehouseId, int Sales, decimal Subtotal, decimal TaxAmount, decimal Total);
public sealed record SalesDocumentSummary(string Number, string? RelatedSaleNumber, string Type, DateTimeOffset Date, string CustomerName, string? SellerName, decimal Subtotal, decimal TaxAmount, decimal Total, decimal Collected, decimal Balance, string PaymentMethod);
public sealed record SupplierDto(Guid Id, string Name, string? TaxIdentifier, string Email, string Phone, string? Notes, long Revision);
public sealed record PurchaseLineDto(Guid Id, Guid ProductId, string Sku, string ProductName, int Quantity, int ReceivedQuantity, decimal UnitCost, decimal LineTotal, decimal TaxRate = 0m, decimal TaxAmount = 0m);
public sealed record PurchaseOrderDto(Guid Id, string Number, string SupplierName, string? Reference, DateOnly? DueDate, PurchaseStatus Status, decimal Total, decimal AmountPaid, decimal BalanceDue, DateTimeOffset CreatedAtUtc, IReadOnlyList<PurchaseLineDto> Lines, IReadOnlyList<SupplierPaymentDto> Payments, Guid WarehouseId, decimal Subtotal = 0m, decimal TaxAmount = 0m);
public sealed record CreateSupplierRequest(string Name, string? TaxIdentifier, string Email, string Phone, string? Notes);
public sealed record UpdateSupplierRequest(string Name, string? TaxIdentifier, string Email, string Phone, string? Notes, long ExpectedRevision);
public sealed record CreatePurchaseLineRequest(Guid ProductId, int Quantity, decimal UnitCost, decimal TaxRate = 0m);
public sealed record CreatePurchaseOrderRequest(Guid SupplierId, string? Reference, IReadOnlyCollection<CreatePurchaseLineRequest> Lines, DateOnly? DueDate = null, Guid WarehouseId = default);
public sealed record InventoryTransferRequest(Guid ProductId, Guid SourceWarehouseId, Guid DestinationWarehouseId, int Quantity, string? Reference = null);
public sealed record InventoryTransferResult(string Reference, int Quantity, Guid ProductId, Guid SourceWarehouseId, int SourceBalance, Guid DestinationWarehouseId, int DestinationBalance);
public sealed record InventoryAdjustmentRequest(Guid IdempotencyKey, Guid ProductId, Guid WarehouseId, int QuantityDelta, decimal? UnitCost, Guid CounterAccountId, string Reason, int? CountedQuantity = null);
public sealed record InventoryAdjustmentDto(Guid Id, string Number, DateTimeOffset CreatedAtUtc, Guid ProductId, string ProductSku, string ProductName,
    Guid WarehouseId, string WarehouseName, Guid ActorUserId, string ActorName, int QuantityDelta, int BalanceBefore, int BalanceAfter,
    decimal UnitCost, decimal ValuationAmount, string Reason, Guid CounterAccountId, string CounterAccountCode, string CounterAccountName, string? JournalEntryNumber, int? CountedQuantity = null);
public sealed record ReceivePurchaseLineRequest(Guid LineId, int Quantity);
public sealed record ReceivePurchaseOrderRequest(Guid IdempotencyKey, IReadOnlyCollection<ReceivePurchaseLineRequest> Lines);
public sealed record SupplierPaymentDto(Guid Id, Guid IdempotencyKey, decimal Amount, SupplierPaymentMethod Method, string? Reference, Guid PaidByUserId, DateTimeOffset CreatedAtUtc);
public sealed record CreateSupplierPaymentRequest(Guid IdempotencyKey, decimal Amount, SupplierPaymentMethod Method, string? Reference, string RegisterCode = "MAIN");
public sealed record DeactivateSupplierRequest(long ExpectedRevision);
public enum PayableAgingBucket { NoDueDate, Current, Days1To30, Days31To60, Days61To90, Over90Days }
public sealed record PayableBalanceDto(Guid PurchaseOrderId, string Number, string SupplierName, string? Reference, DateOnly? DueDate, decimal Total, decimal AmountPaid, decimal BalanceDue);
public sealed record PayableAgingLineDto(Guid PurchaseOrderId, string Number, string SupplierName, string? Reference, DateOnly? DueDate, decimal Total, decimal AmountPaid, decimal BalanceDue, int DaysPastDue, PayableAgingBucket Bucket);
public sealed record PayableAgingBand(PayableAgingBucket Bucket, int Count, decimal Balance);
public sealed record PayablesAgingReport(DateOnly AsOfDate, int OpenItems, decimal TotalBalance, IReadOnlyList<PayableAgingBand> Bands, IReadOnlyList<PayableAgingLineDto> Items);
public sealed record CashSessionDto(Guid Id, string RegisterCode, CashSessionStatus Status, decimal OpeningFloat, Guid OpenedByUserId, DateTimeOffset OpenedAtUtc, decimal CashSales, decimal CashIn, decimal CashOut, decimal ExpectedCash, DateTimeOffset? ClosedAtUtc, decimal? CountedCash, decimal? Difference, IReadOnlyList<CashMovementDto> Movements);
public sealed record CashMovementDto(Guid Id, CashMovementKind Kind, decimal Amount, string Description, Guid UserId, DateTimeOffset CreatedAtUtc);
public sealed record OpenCashSessionRequest(string RegisterCode, decimal OpeningFloat);
public sealed record CashMovementRequest(CashMovementKind Kind, decimal Amount, string Description,
    Guid CashPaymentMethodId, Guid CounterAccountId, Guid IdempotencyKey);
public sealed record CloseCashSessionRequest(decimal CountedCash, Guid? CashPaymentMethodId = null, Guid? DifferenceAccountId = null);
public sealed record SalePaymentDto(string MethodName, PaymentMethodType Type, decimal Amount, string? Reference);
public sealed record SaleReturnDto(Guid Id, string Number, decimal Subtotal, decimal TaxAmount, decimal Total, decimal ReceivableCredit, decimal RefundAmount, DateTimeOffset CreatedAtUtc, IReadOnlyList<SaleReturnLineDto> Lines);
public sealed record SaleReturnLineDto(Guid SaleLineId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal TaxRate, decimal LineTotal, decimal TaxAmount, decimal DiscountPercent = 0m, decimal DiscountAmount = 0m);
public sealed record SaleReceiptDto(Guid Id, string Number, string CustomerName, decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total, PaymentMethod PaymentMethod, SaleStatus Status, DateTimeOffset CreatedAtUtc, string? RegisterCode, IReadOnlyList<ReceiptLineDto> Lines, IReadOnlyList<SalePaymentDto>? Payments = null, string? SellerName = null, IReadOnlyList<SaleReturnDto>? Returns = null, decimal BalanceDue = 0m, decimal DiscountAmount = 0m);
public sealed record ReceiptLineDto(Guid SaleLineId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, decimal TaxRate = 0m, decimal TaxAmount = 0m, decimal DiscountPercent = 0m, decimal DiscountAmount = 0m);
public sealed record SalesReport(DateOnly From, DateOnly To, int SalesCount, decimal Subtotal, decimal TaxAmount, decimal Total,
    IReadOnlyList<PaymentBreakdown> Payments, IReadOnlyList<ProductSalesSummary> TopProducts, IReadOnlyList<DailySalesSummary> DailySales,
    IReadOnlyList<SellerSalesSummary>? Sellers = null, IReadOnlyList<CustomerSalesSummary>? Customers = null,
    IReadOnlyList<WarehouseSalesSummary>? Warehouses = null, IReadOnlyList<SalesDocumentSummary>? Documents = null);
public sealed record CashSessionReport(DateOnly From, DateOnly To, int Sessions, int OpenSessions, decimal CashSales, decimal CashIn,
    decimal CashOut, decimal CountedDifference, IReadOnlyList<CashSessionDto> Items);
public sealed record OpportunityDto(Guid Id, Guid CustomerId, string CustomerName, string Title, decimal ExpectedValue, DateOnly? ExpectedCloseDate, string? Description, OpportunityStage Stage, DateTimeOffset CreatedAtUtc, Guid? AssignedToUserId = null, string? AssignedToName = null);
public sealed record CreateOpportunityRequest(Guid CustomerId, string Title, decimal ExpectedValue, DateOnly? ExpectedCloseDate, string? Description, Guid? AssignedToUserId = null);
public sealed record MoveOpportunityRequest(OpportunityStage Stage);
public sealed record CrmActivityDto(Guid Id, Guid CustomerId, string CustomerName, Guid? OpportunityId, Guid CreatedByUserId, CrmActivityKind Kind, string Subject, string? Details, DateTimeOffset OccurredAtUtc, DateTimeOffset? DueAtUtc, DateTimeOffset? CompletedAtUtc, Guid? AssignedToUserId = null, string? AssignedToName = null)
{
    public bool IsCompleted => CompletedAtUtc is not null;
}
public sealed record CreateCrmActivityRequest(Guid CustomerId, Guid? OpportunityId, CrmActivityKind Kind, string Subject, string? Details, DateTimeOffset? DueAtUtc, Guid? AssignedToUserId = null);
