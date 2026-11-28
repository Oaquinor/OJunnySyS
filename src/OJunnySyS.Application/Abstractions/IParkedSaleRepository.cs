using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Abstractions;

public interface IParkedSaleRepository
{
    Task<IReadOnlyList<ParkedSale>> GetParkedSalesAsync(string registerCode, CancellationToken cancellationToken = default);
    Task<ParkedSale> ParkSaleAsync(ParkSaleRequest request, Guid cashierUserId, CancellationToken cancellationToken = default);
    Task<ParkedSale> CancelParkedSaleAsync(Guid id, Guid actorUserId, CancelParkedSaleRequest request, CancellationToken cancellationToken = default);
}
