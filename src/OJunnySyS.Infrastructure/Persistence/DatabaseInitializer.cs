using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Infrastructure.Identity;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<StoreDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        db.CurrentTenantId = TenantDefaults.LocalDemoTenantId;
        var warehouse = await db.Warehouses.SingleAsync(x => x.Code == "MAIN", cancellationToken);
        if (await db.Products.AnyAsync(cancellationToken)) return;

        var products = new[]
        {
            new Product(TenantDefaults.LocalDemoTenantId, "CAF-001", "Café de origen · 250 g", "Café", 480m, 24, "746000000001"),
            new Product(TenantDefaults.LocalDemoTenantId, "CAF-002", "Cold brew clásico", "Bebidas", 225m, 18, "746000000002"),
            new Product(TenantDefaults.LocalDemoTenantId, "ACC-001", "Vaso térmico Ojunny", "Accesorios", 890m, 7, "746000000003"),
            new Product(TenantDefaults.LocalDemoTenantId, "CAF-003", "Caja degustación", "Regalos", 1350m, 4, "746000000004"),
            new Product(TenantDefaults.LocalDemoTenantId, "ACC-002", "Filtro reutilizable", "Accesorios", 390m, 16, "746000000005")
        };
        db.Products.AddRange(products);
        db.WarehouseStocks.AddRange(products.Select(x => new WarehouseStock(x.TenantId, warehouse.Id, x.Id, x.StockOnHand)));
        db.InventoryMovements.AddRange(products.Select(x => new InventoryMovement(x.TenantId, x.Id, InventoryMovementType.StockAdjustment, x.StockOnHand, x.StockOnHand, "Existencias iniciales de demostración", warehouse.Id)));
        db.Customers.AddRange(
            new Customer(TenantDefaults.LocalDemoTenantId, "María", "Fernández", "maria.fernandez@example.com", "809-555-0142", "Estudio Norte"),
            new Customer(TenantDefaults.LocalDemoTenantId, "Carlos", "Méndez", "carlos.mendez@example.com", "829-555-0188"),
            new Customer(TenantDefaults.LocalDemoTenantId, "Sofía", "Ramírez", "sofia.ramirez@example.com", "849-555-0116", "Casa Palma"));
        await db.SaveChangesAsync(cancellationToken);
    }
}
