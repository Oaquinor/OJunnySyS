using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class SaleReturnTests
{
    [TestMethod]
    public void Partial_credit_note_reduces_receivable_and_preserves_original_sale()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Ana", "Demo", "ana@example.test", "8095550101");
        var line = new SaleLine(Guid.NewGuid(), "SKU-1", "Artículo", 50m, 2, .18m, 20m);
        var sale = new Sale(tenantId, "V-100", customer, PaymentMethod.Credit, [line], null, Guid.NewGuid(), .18m,
            Guid.NewGuid(), "MAIN", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
        sale.RecordPayments([]);
        var returnId = Guid.NewGuid();
        var creditLine = new SaleReturnLine(tenantId, Guid.NewGuid(), returnId, line.Id, line.ProductId,
            line.Sku, line.ProductName, 1, 1, line.UnitPrice, line.TaxRate, line.UnitCost);
        var creditNote = new SaleReturn(tenantId, returnId, sale.Id, "NC-100", Guid.NewGuid(), [creditLine],
            59m, 0m, null, null, null, null, null);

        sale.RecordReturn(creditNote);

        Assert.AreEqual(118m, sale.Total);
        Assert.AreEqual(59m, sale.ReturnedTotal);
        Assert.AreEqual(59m, sale.BalanceDue);
    }

    [TestMethod]
    public void Return_distribution_must_equal_credit_note_total()
    {
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();
        var returnId = Guid.NewGuid();
        var line = new SaleReturnLine(tenantId, Guid.NewGuid(), returnId, saleLineId, Guid.NewGuid(),
            "SKU-1", "Artículo", 1, 1, 50m, .18m, 20m);

        Assert.Throws<OJunnySyS.Domain.Common.DomainException>(() => new SaleReturn(tenantId, returnId, saleId,
            "NC-101", Guid.NewGuid(), [line], 50m, 0m, null, null, null, null, null));
    }

    [TestMethod]
    public void Sale_rejects_returning_more_units_than_were_sold()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Ana", "Demo", "ana@example.test", "8095550101");
        var line = new SaleLine(Guid.NewGuid(), "SKU-2", "Artículo", 50m, 2, .18m, 20m);
        var sale = new Sale(tenantId, "V-101", customer, PaymentMethod.Credit, [line], null, Guid.NewGuid(), .18m,
            Guid.NewGuid(), "MAIN", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
        var firstId = Guid.NewGuid();
        var firstLine = new SaleReturnLine(tenantId, Guid.NewGuid(), firstId, line.Id, line.ProductId, line.Sku,
            line.ProductName, 1, 1, line.UnitPrice, line.TaxRate, line.UnitCost);
        sale.RecordReturn(new SaleReturn(tenantId, firstId, sale.Id, "NC-102", Guid.NewGuid(), [firstLine], 59m, 0m, null, null, null, null, null));

        var secondId = Guid.NewGuid();
        var secondLine = new SaleReturnLine(tenantId, Guid.NewGuid(), secondId, line.Id, line.ProductId, line.Sku,
            line.ProductName, 2, 2, line.UnitPrice, line.TaxRate, line.UnitCost);
        var secondCreditNote = new SaleReturn(tenantId, secondId, sale.Id, "NC-103", Guid.NewGuid(), [secondLine], 118m, 0m, null, null, null, null, null);

        Assert.Throws<OJunnySyS.Domain.Common.DomainException>(() => sale.RecordReturn(secondCreditNote));
    }

    [TestMethod]
    public void Partial_returns_allocate_discount_and_tax_rounding_without_exceeding_original_line()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Ana", "Demo", "discount-return@example.test", "8095550101");
        var line = new SaleLine(Guid.NewGuid(), "SKU-CENTS", "Artículo de centavos", .05m, 3, .18m, 0m, 3, 10m);
        var sale = new Sale(tenantId, "V-CENTS", customer, PaymentMethod.Credit, [line], null, Guid.NewGuid(), .18m,
            Guid.NewGuid(), "MAIN", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
        sale.RecordPayments([]);

        var expectedSubtotals = new[] { .04m, .05m, .04m };
        var expectedTaxes = new[] { .01m, 0m, .01m };
        for (var index = 0; index < 3; index++)
        {
            var returnId = Guid.NewGuid();
            var returned = new SaleReturnLine(tenantId, Guid.NewGuid(), returnId, line.Id, line.ProductId, line.Sku,
                line.ProductName, 1, 0, line.UnitPrice, line.TaxRate, line.UnitCost, line.DiscountPercent,
                expectedSubtotals[index], expectedTaxes[index]);
            var amount = expectedSubtotals[index] + expectedTaxes[index];
            sale.RecordReturn(new SaleReturn(tenantId, returnId, sale.Id, $"NC-CENTS-{index}", Guid.NewGuid(), [returned],
                amount, 0m, null, null, null, null, null));
        }

        Assert.AreEqual(line.LineTotal, sale.Returns.Sum(x => x.Subtotal));
        Assert.AreEqual(line.TaxAmount, sale.Returns.Sum(x => x.TaxAmount));
        Assert.AreEqual(0m, sale.BalanceDue);
    }
}
