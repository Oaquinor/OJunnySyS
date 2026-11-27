using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class CustomerStatementBuilderTests
{
    [TestMethod]
    public void Build_CarriesOpeningBalanceAndOrdersMovementsWithRunningBalance()
    {
        var customerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var data = new CustomerStatementData(118m,
        [
            new CustomerStatementMovement(saleId, DateTimeOffset.Parse("2026-11-05T10:00:00Z"), "V-002", CustomerStatementEntryType.Payment, "Abono", null, 0m, 40m),
            new CustomerStatementMovement(saleId, DateTimeOffset.Parse("2026-11-05T09:00:00Z"), "V-002", CustomerStatementEntryType.Sale, "Venta a crédito", new DateOnly(2026, 12, 5), 75m, 0m)
        ]);

        var report = CustomerStatementBuilder.Build(customerId, "Cliente Demo", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), data);

        Assert.AreEqual(118m, report.OpeningBalance);
        Assert.AreEqual(75m, report.Invoiced);
        Assert.AreEqual(40m, report.Collected);
        Assert.AreEqual(153m, report.ClosingBalance);
        Assert.AreEqual(CustomerStatementEntryType.Sale, report.Entries[0].Type);
        Assert.AreEqual(193m, report.Entries[0].Balance);
        Assert.AreEqual(153m, report.Entries[1].Balance);
    }

    [TestMethod]
    public void Build_EmptyPeriodKeepsOpeningBalanceAsClosingBalance()
    {
        var report = CustomerStatementBuilder.Build(Guid.NewGuid(), "Cliente Demo", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), new CustomerStatementData(52.347m, []));

        Assert.AreEqual(52.35m, report.OpeningBalance);
        Assert.AreEqual(52.35m, report.ClosingBalance);
        Assert.AreEqual(0m, report.Invoiced);
        Assert.AreEqual(0m, report.Collected);
        Assert.AreEqual(0, report.Entries.Count);
    }
}
