using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Services;

public sealed class ParkedSaleService(IParkedSaleRepository repository)
{
    public async Task<IReadOnlyList<ParkedSaleDto>> GetParkedSalesAsync(string registerCode, CancellationToken ct = default) =>
        (await repository.GetParkedSalesAsync(registerCode, ct)).Select(Map).ToArray();

    public async Task<ParkedSaleDto> ParkSaleAsync(ParkSaleRequest request, Guid cashierUserId, CancellationToken ct = default) =>
        Map(await repository.ParkSaleAsync(request, cashierUserId, ct));

    public async Task<ParkedSaleDto> CancelParkedSaleAsync(Guid id, Guid actorUserId, CancelParkedSaleRequest request, CancellationToken ct = default) =>
        Map(await repository.CancelParkedSaleAsync(id, actorUserId, request, ct));

    private static ParkedSaleDto Map(ParkedSale parked) => new(parked.Id, parked.RegisterCode, parked.CashierUserId,
        parked.CustomerId, parked.CustomerName, parked.SellerUserId, parked.SellerName, parked.Status, parked.ConvertedSaleId,
        parked.CreatedAtUtc, parked.Revision, parked.Lines.OrderBy(x => x.ProductName)
            .Select(x => new ParkedSaleLineDto(x.ProductId, x.Sku, x.ProductName, x.Quantity, x.DiscountPercent)).ToArray());
}
