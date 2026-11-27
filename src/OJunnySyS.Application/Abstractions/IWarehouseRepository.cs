using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Application.Abstractions;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<Warehouse> CreateWarehouseAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<Warehouse> UpdateWarehouseAsync(Guid id, string name, long expectedRevision, CancellationToken cancellationToken = default);
    Task<Warehouse> SetWarehouseActiveAsync(Guid id, bool isActive, long expectedRevision, CancellationToken cancellationToken = default);
}
