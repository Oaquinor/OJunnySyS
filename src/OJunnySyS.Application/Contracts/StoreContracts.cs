using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Crm;

namespace OJunnySyS.Application.Contracts;

public sealed record ProductDto(Guid Id, string Sku, string Name, string Category, decimal UnitPrice, int StockOnHand, string? Barcode, bool IsActive = true, long Revision = 0);
public sealed record UpdateProductRequest(string Sku, string Name, string Category, decimal UnitPrice, string? Barcode, long ExpectedRevision);
public sealed record SetProductActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record CustomerDto(Guid Id, string Name, string Email, string Phone, string? Company, decimal LifetimeValue, long Revision, string FirstName, string LastName, DateTimeOffset CreatedAtUtc = default);
public sealed record UpdateCustomerRequest(string FirstName, string LastName, string Email, string Phone, string? Company, long ExpectedRevision);
public sealed record DeactivateCustomerRequest(long ExpectedRevision);
public sealed record SaleDto(Guid Id, string Number, string CustomerName, decimal Subtotal, decimal TaxAmount, decimal Total, PaymentMethod PaymentMethod, DateTimeOffset CreatedAtUtc, SaleStatus Status = SaleStatus.Completed);
public sealed record CheckoutLine(Guid ProductId, int Quantity);
public sealed record CheckoutRequest(Guid? CustomerId, PaymentMethod PaymentMethod, IReadOnlyCollection<CheckoutLine> Lines, Guid IdempotencyKey = default);
public sealed record DashboardSummary(decimal SalesToday, int OrdersToday, int ActiveCustomers, int LowStockProducts, IReadOnlyList<SaleDto> RecentSales);
public sealed record PaymentBreakdown(PaymentMethod Method, int Sales, decimal Total);
public sealed record ProductSalesSummary(string ProductName, int Quantity, decimal Revenue);
public sealed record DailySalesSummary(DateOnly Date, decimal Total);
public sealed record SupplierDto(Guid Id, string Name, string? TaxIdentifier, string Email, string Phone, string? Notes, long Revision);
public sealed record PurchaseLineDto(Guid Id, Guid ProductId, string Sku, string ProductName, int Quantity, int ReceivedQuantity, decimal UnitCost, decimal LineTotal);
public sealed record PurchaseOrderDto(Guid Id, string Number, string SupplierName, string? Reference, DateOnly? DueDate, PurchaseStatus Status, decimal Total, decimal AmountPaid, decimal BalanceDue, DateTimeOffset CreatedAtUtc, IReadOnlyList<PurchaseLineDto> Lines, IReadOnlyList<SupplierPaymentDto> Payments);
public sealed record CreateSupplierRequest(string Name, string? TaxIdentifier, string Email, string Phone, string? Notes);
public sealed record UpdateSupplierRequest(string Name, string? TaxIdentifier, string Email, string Phone, string? Notes, long ExpectedRevision);
public sealed record CreatePurchaseLineRequest(Guid ProductId, int Quantity, decimal UnitCost);
public sealed record CreatePurchaseOrderRequest(Guid SupplierId, string? Reference, IReadOnlyCollection<CreatePurchaseLineRequest> Lines, DateOnly? DueDate = null);
public sealed record ReceivePurchaseLineRequest(Guid LineId, int Quantity);
public sealed record SupplierPaymentDto(Guid Id, Guid IdempotencyKey, decimal Amount, SupplierPaymentMethod Method, string? Reference, Guid PaidByUserId, DateTimeOffset CreatedAtUtc);
public sealed record CreateSupplierPaymentRequest(Guid IdempotencyKey, decimal Amount, SupplierPaymentMethod Method, string? Reference);
public sealed record DeactivateSupplierRequest(long ExpectedRevision);
public enum PayableAgingBucket { NoDueDate, Current, Days1To30, Days31To60, Days61To90, Over90Days }
public sealed record PayableBalanceDto(Guid PurchaseOrderId, string Number, string SupplierName, string? Reference, DateOnly? DueDate, decimal Total, decimal AmountPaid, decimal BalanceDue);
public sealed record PayableAgingLineDto(Guid PurchaseOrderId, string Number, string SupplierName, string? Reference, DateOnly? DueDate, decimal Total, decimal AmountPaid, decimal BalanceDue, int DaysPastDue, PayableAgingBucket Bucket);
public sealed record PayableAgingBand(PayableAgingBucket Bucket, int Count, decimal Balance);
public sealed record PayablesAgingReport(DateOnly AsOfDate, int OpenItems, decimal TotalBalance, IReadOnlyList<PayableAgingBand> Bands, IReadOnlyList<PayableAgingLineDto> Items);
public sealed record CashSessionDto(Guid Id, string RegisterCode, CashSessionStatus Status, decimal OpeningFloat, Guid OpenedByUserId, DateTimeOffset OpenedAtUtc, decimal CashSales, decimal CashIn, decimal CashOut, decimal ExpectedCash, DateTimeOffset? ClosedAtUtc, decimal? CountedCash, decimal? Difference, IReadOnlyList<CashMovementDto> Movements);
public sealed record CashMovementDto(Guid Id, CashMovementKind Kind, decimal Amount, string Description, Guid UserId, DateTimeOffset CreatedAtUtc);
public sealed record OpenCashSessionRequest(string RegisterCode, decimal OpeningFloat);
public sealed record CashMovementRequest(CashMovementKind Kind, decimal Amount, string Description);
public sealed record CloseCashSessionRequest(decimal CountedCash);
public sealed record SaleReceiptDto(Guid Id, string Number, string CustomerName, decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total, PaymentMethod PaymentMethod, SaleStatus Status, DateTimeOffset CreatedAtUtc, string? RegisterCode, IReadOnlyList<ReceiptLineDto> Lines);
public sealed record ReceiptLineDto(string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record SalesReport(DateOnly From, DateOnly To, int SalesCount, decimal Subtotal, decimal TaxAmount, decimal Total,
    IReadOnlyList<PaymentBreakdown> Payments, IReadOnlyList<ProductSalesSummary> TopProducts, IReadOnlyList<DailySalesSummary> DailySales);
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
