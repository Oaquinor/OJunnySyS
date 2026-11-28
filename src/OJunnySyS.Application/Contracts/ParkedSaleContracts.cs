using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Contracts;

public sealed record ParkedSaleLineRequest(Guid ProductId, int Quantity, decimal DiscountPercent = 0m);
public sealed record ParkSaleRequest(Guid IdempotencyKey, string RegisterCode, Guid? CustomerId, Guid? SellerUserId,
    IReadOnlyCollection<ParkedSaleLineRequest> Lines);
public sealed record CancelParkedSaleRequest(long ExpectedRevision);
public sealed record ParkedSaleLineDto(Guid ProductId, string Sku, string ProductName, int Quantity, decimal DiscountPercent);
public sealed record ParkedSaleDto(Guid Id, string RegisterCode, Guid CashierUserId, Guid? CustomerId, string CustomerName,
    Guid? SellerUserId, string? SellerName, ParkedSaleStatus Status, Guid? ConvertedSaleId, DateTimeOffset CreatedAtUtc,
    long Revision, IReadOnlyList<ParkedSaleLineDto> Lines);
