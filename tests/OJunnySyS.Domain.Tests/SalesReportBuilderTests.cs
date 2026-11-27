using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class SalesReportBuilderTests
{
    [TestMethod]
    public void Build_DeductsCreditNoteFromNetSalesMarginAndRefundMethod()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Ana", "Demo", "ana@example.test", "8095550101");
        var line = new SaleLine(Guid.NewGuid(), "SKU-REPORT", "Artículo", 50m, 2, .18m, 20m);
        var cashMethodId = Guid.NewGuid();
        var sale = new Sale(tenantId, "V-REPORT", customer, PaymentMethod.Cash, [line], Guid.NewGuid(), Guid.NewGuid(), .18m,
            Guid.NewGuid(), "MAIN", warehouseId);
        sale.RecordPayments([new PaymentAllocation(cashMethodId, "Efectivo", PaymentMethodType.Cash, 118m, Guid.NewGuid())]);
        var returnId = Guid.NewGuid();
        var returnLine = new SaleReturnLine(tenantId, Guid.NewGuid(), returnId, line.Id, line.ProductId,
            line.Sku, line.ProductName, 1, 1, line.UnitPrice, line.TaxRate, line.UnitCost);
        sale.RecordReturn(new SaleReturn(tenantId, returnId, sale.Id, "NC-REPORT", Guid.NewGuid(), [returnLine],
            0m, 59m, Guid.NewGuid(), "Transferencia", Guid.NewGuid(), PaymentMethodType.Transfer, null));

        var clock = new UtcBusinessClock();
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var (start, end) = clock.GetUtcRange(today, today);
        var report = SalesReportBuilder.Build(today, today, start, end, [sale], clock);

        Assert.AreEqual(1, report.SalesCount);
        Assert.AreEqual(50m, report.Subtotal);
        Assert.AreEqual(9m, report.TaxAmount);
        Assert.AreEqual(59m, report.Total);
        Assert.AreEqual(1, report.TopProducts.Count);
        Assert.AreEqual(1, report.TopProducts[0].Quantity);
        Assert.AreEqual(50m, report.TopProducts[0].Revenue);
        Assert.AreEqual(20m, report.TopProducts[0].Cost);
        Assert.AreEqual(30m, report.TopProducts[0].GrossProfit);
        Assert.AreEqual(-59m, report.Payments.Single(x => x.Method == "Transferencia").Total);
        Assert.AreEqual(59m, report.Customers!.Single().Total);
        Assert.AreEqual(59m, report.Warehouses!.Single().Total);
        Assert.AreEqual(59m, report.DailySales.Single().Total);
    }

    private sealed class UtcBusinessClock : IBusinessClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset ToBusinessTime(DateTimeOffset instantUtc) => instantUtc;
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) CurrentBusinessDayUtc => GetUtcRange(DateOnly.FromDateTime(UtcNow.UtcDateTime), DateOnly.FromDateTime(UtcNow.UtcDateTime));
        public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly startInclusive, DateOnly endInclusive) =>
            (new DateTimeOffset(startInclusive.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(endInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
    }
}
