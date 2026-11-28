using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class SalesQuoteTests
{
    [TestMethod]
    public void Draft_can_be_revised_and_sent_but_sent_quote_cannot_be_edited()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Ana", "Demo", "ana-quote@example.test", "8095550101");
        var line = new SalesQuoteLine(tenantId, Guid.NewGuid(), "SKU-1", "Artículo", 2, 90m, .18m);
        var today = new DateOnly(2026, 11, 27);
        var quote = new SalesQuote(tenantId, "COT-20261127-0001", customer, today.AddDays(15), null, [line]);

        quote.UpdateDraft(customer, today.AddDays(20), "Condiciones acordadas", [line], 0);
        quote.MarkSent(1, today);

        Assert.AreEqual(SalesQuoteStatus.Sent, quote.Status);
        Assert.AreEqual(180m, quote.Subtotal);
        Assert.AreEqual(32.40m, quote.TaxAmount);
        Assert.AreEqual(212.40m, quote.Total);
        Assert.Throws<DomainException>(() => quote.UpdateDraft(customer, today.AddDays(10), null, [line], quote.Revision));
    }

    [TestMethod]
    public void Expired_sent_quote_cannot_be_accepted_and_only_accepted_quote_can_convert()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Luis", "Prueba", "luis-quote@example.test", "8095550102");
        var today = new DateOnly(2026, 11, 27);
        var quote = new SalesQuote(tenantId, "COT-20261127-0002", customer, today, null,
            [new SalesQuoteLine(tenantId, Guid.NewGuid(), "SKU-2", "Artículo", 1, 25m, 0m)]);
        quote.MarkSent(0, today);

        Assert.Throws<DomainException>(() => quote.Accept(1, today.AddDays(1)));
        quote.Accept(1, today);
        quote.MarkConverted(Guid.NewGuid());

        Assert.AreEqual(SalesQuoteStatus.Converted, quote.Status);
        Assert.IsNotNull(quote.ConvertedSaleId);
        Assert.Throws<DomainException>(() => quote.MarkConverted(Guid.NewGuid()));
    }

    [TestMethod]
    public void Quote_enforces_tenant_and_revision_when_it_is_edited()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Marta", "Demo", "marta-quote@example.test", "8095550103");
        var quote = new SalesQuote(tenantId, "COT-20261127-0003", customer, new DateOnly(2026, 12, 10), null,
            [new SalesQuoteLine(tenantId, Guid.NewGuid(), "SKU-3", "Artículo", 1, 40m, .18m)]);

        Assert.Throws<ConcurrencyConflictException>(() => quote.Cancel(4));
        Assert.Throws<DomainException>(() => quote.UpdateDraft(customer, new DateOnly(2026, 12, 10), null,
            [new SalesQuoteLine(Guid.NewGuid(), Guid.NewGuid(), "SKU-4", "Otro", 1, 10m, 0m)], 0));
    }
}
