using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Abstractions;

public interface ISalesQuoteRepository
{
    Task<IReadOnlyList<SalesQuote>> GetAsync(CancellationToken cancellationToken = default);
    Task<SalesQuote> CreateAsync(CreateSalesQuoteRequest request, DateOnly today, CancellationToken cancellationToken = default);
    Task<SalesQuote> UpdateAsync(Guid id, UpdateSalesQuoteRequest request, DateOnly today, CancellationToken cancellationToken = default);
    Task<SalesQuote> MarkSentAsync(Guid id, SalesQuoteStatusRequest request, DateOnly today, CancellationToken cancellationToken = default);
    Task<SalesQuote> AcceptAsync(Guid id, SalesQuoteStatusRequest request, DateOnly today, CancellationToken cancellationToken = default);
    Task<SalesQuote> RejectAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken cancellationToken = default);
    Task<SalesQuote> CancelAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken cancellationToken = default);
    Task<Sale> ConvertAsync(Guid id, Guid actorId, ConvertSalesQuoteRequest request, CancellationToken cancellationToken = default);
}
