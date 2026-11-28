using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Common;

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
}
