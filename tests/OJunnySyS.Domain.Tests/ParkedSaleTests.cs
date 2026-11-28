using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class ParkedSaleTests
{
    [TestMethod]
    public void Parked_sale_can_be_cancelled_once_with_optimistic_revision()
    {
        var tenantId = Guid.NewGuid();
        var parkedId = Guid.NewGuid();
        var parked = CreateParkedSale(tenantId, parkedId);

        parked.Cancel(Guid.NewGuid(), 0);

        Assert.AreEqual(ParkedSaleStatus.Cancelled, parked.Status);
        Assert.AreEqual(1L, parked.Revision);
        Assert.Throws<DomainException>(() => parked.Cancel(Guid.NewGuid(), 1));
        Assert.Throws<ConcurrencyConflictException>(() => parked.Cancel(Guid.NewGuid(), 0));
    }

    [TestMethod]
    public void Parked_sale_can_be_completed_once_and_keeps_sale_link()
    {
        var parked = CreateParkedSale(Guid.NewGuid(), Guid.NewGuid());
        var saleId = Guid.NewGuid();

        parked.Complete(saleId);

        Assert.AreEqual(ParkedSaleStatus.Completed, parked.Status);
        Assert.AreEqual(saleId, parked.ConvertedSaleId);
        Assert.AreEqual(1L, parked.Revision);
        Assert.Throws<DomainException>(() => parked.Complete(Guid.NewGuid()));
    }

    private static ParkedSale CreateParkedSale(Guid tenantId, Guid parkedId)
    {
        var line = new ParkedSaleLine(tenantId, Guid.NewGuid(), parkedId, Guid.NewGuid(), "ITEM-1", "Producto", 2, 5m);
        return new ParkedSale(tenantId, parkedId, Guid.NewGuid(), "MAIN", Guid.NewGuid(), null, "", null, null, [line]);
    }
}
