using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Application.Contracts;

public sealed record WarehouseDto(Guid Id, string Code, string Name, bool IsActive, long Revision, DateTimeOffset CreatedAtUtc);
public sealed record CreateWarehouseRequest(string Code, string Name);
public sealed record UpdateWarehouseRequest(string Name, long ExpectedRevision);
public sealed record SetWarehouseActiveRequest(bool IsActive, long ExpectedRevision);
public sealed record ProductStockSnapshot(Product Product, int WarehouseStock);

public static class WarehouseMappings
{
    public static WarehouseDto ToDto(this Warehouse warehouse) =>
        new(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.IsActive, warehouse.Revision, warehouse.CreatedAtUtc);
}
