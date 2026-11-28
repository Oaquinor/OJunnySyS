using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Services;

public sealed class SalesQuoteService(ISalesQuoteRepository repository, IBusinessClock clock)
{
    public async Task<IReadOnlyList<SalesQuoteDto>> GetAsync(CancellationToken ct = default) =>
        (await repository.GetAsync(ct)).Select(Map).ToArray();

    public async Task<SalesQuoteDto> CreateAsync(CreateSalesQuoteRequest request, CancellationToken ct = default) =>
        Map(await repository.CreateAsync(request, Today(), ct));

    public async Task<SalesQuoteDto> UpdateAsync(Guid id, UpdateSalesQuoteRequest request, CancellationToken ct = default) =>
        Map(await repository.UpdateAsync(id, request, Today(), ct));

    public async Task<SalesQuoteDto> MarkSentAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken ct = default) =>
        Map(await repository.MarkSentAsync(id, request, Today(), ct));

    public async Task<SalesQuoteDto> AcceptAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken ct = default) =>
        Map(await repository.AcceptAsync(id, request, Today(), ct));

    public async Task<SalesQuoteDto> RejectAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken ct = default) =>
        Map(await repository.RejectAsync(id, request, ct));

    public async Task<SalesQuoteDto> CancelAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken ct = default) =>
        Map(await repository.CancelAsync(id, request, ct));

    public async Task<SaleDto> ConvertAsync(Guid id, Guid actorId, ConvertSalesQuoteRequest request, CancellationToken ct = default)
    {
        var sale = await repository.ConvertAsync(id, actorId, request, ct);
        return new SaleDto(sale.Id, sale.Number, sale.CustomerName, sale.Subtotal, sale.TaxAmount, sale.Total,
            sale.PaymentMethod, sale.CreatedAtUtc, sale.Status, "Crédito", sale.SellerName, sale.DueDate, sale.AmountPaid, sale.BalanceDue);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime);

    private static SalesQuoteDto Map(SalesQuote quote) => new(quote.Id, quote.Number, quote.CustomerId, quote.CustomerName,
        quote.ValidUntil, quote.Notes, quote.Status, quote.Subtotal, quote.TaxAmount, quote.Total, quote.CreatedAtUtc,
        quote.Revision, quote.ConvertedSaleId, quote.Lines.OrderBy(x => x.CreatedAtUtc).Select(x => new SalesQuoteLineDto(
            x.ProductId, x.Sku, x.ProductName, x.Quantity, x.UnitPrice, x.LineTotal, x.TaxRate, x.TaxAmount)).ToArray());
}
