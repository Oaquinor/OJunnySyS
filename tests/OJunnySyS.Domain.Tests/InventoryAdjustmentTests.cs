using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class InventoryAdjustmentTests
{
    [TestMethod]
    public void Increase_adjustment_uses_weighted_cost_and_decrease_preserves_global_stock_invariant()
    {
        var product = new Product(Guid.NewGuid(), "ADJ-1", "Producto", "General", 100m, 10, initialUnitCost: 20m);

        product.ApplyInventoryAdjustment(2, 30m);
        Assert.AreEqual(12, product.StockOnHand);
        Assert.AreEqual(21.6667m, product.UnitCost);
        Assert.AreEqual(0, product.UncostedQuantity);

        product.ApplyInventoryAdjustment(-3, null);
        Assert.AreEqual(9, product.StockOnHand);
        Assert.AreEqual(21.6667m, product.UnitCost);
    }

    [TestMethod]
    public void Zero_cost_increase_is_tracked_as_uncosted_and_removed_before_costed_inventory()
    {
        var product = new Product(Guid.NewGuid(), "ADJ-2", "Producto sin costo", "General", 50m, 4);

        product.ApplyInventoryAdjustment(2, 0m);
        Assert.AreEqual(6, product.StockOnHand);
        Assert.AreEqual(6, product.UncostedQuantity);

        product.ApplyInventoryAdjustment(-3, null);
        Assert.AreEqual(3, product.StockOnHand);
        Assert.AreEqual(3, product.UncostedQuantity);
    }

    [TestMethod]
    public void Adjustment_rejects_zero_or_unbalanced_cost_direction()
    {
        var product = new Product(Guid.NewGuid(), "ADJ-3", "Producto", "General", 50m, 2, initialUnitCost: 10m);

        Assert.Throws<DomainException>(() => product.ApplyInventoryAdjustment(0, 10m));
        Assert.Throws<DomainException>(() => product.ApplyInventoryAdjustment(-1, 10m));
        Assert.Throws<DomainException>(() => product.ApplyInventoryAdjustment(1, null));
        Assert.Throws<DomainException>(() => product.ApplyInventoryAdjustment(-3, null));
        Assert.AreEqual(2, product.StockOnHand);
    }

    [TestMethod]
    public void Physical_count_can_record_matching_stock_and_rejects_a_mismatched_snapshot()
    {
        var tenantId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var verification = new InventoryAdjustment(tenantId, "IA-COUNT-1", Guid.NewGuid(), new string('A', 64),
            productId, "COUNT-1", "Producto contado", warehouseId, "Almacén", actorId, "Supervisor", 0, 7, 7,
            12m, 0m, "Conteo físico sin diferencias", accountId, "4190", "Ganancias por sobrante", null, 7);
        Assert.AreEqual(7, verification.CountedQuantity);
        Assert.AreEqual(0, verification.QuantityDelta);

        Assert.Throws<DomainException>(() => new InventoryAdjustment(tenantId, "IA-COUNT-2", Guid.NewGuid(), new string('B', 64),
            productId, "COUNT-1", "Producto contado", warehouseId, "Almacén", actorId, "Supervisor", 0, 7, 7,
            12m, 0m, "Conteo físico", accountId, "4190", "Ganancias por sobrante", null, 8));
    }
}
