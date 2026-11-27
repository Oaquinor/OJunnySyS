using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class CreditSaleTests
{
    [TestMethod]
    public void Credit_sale_without_cash_session_keeps_full_balance_open()
    {
        var sale = CreateCreditSale();
        sale.RecordPayments([]);

        Assert.IsNull(sale.CashSessionId);
        Assert.AreEqual(118m, sale.Total);
        Assert.AreEqual(0m, sale.AmountPaid);
        Assert.AreEqual(118m, sale.BalanceDue);
    }

    [TestMethod]
    public void Credit_sale_requires_customer_and_due_date()
    {
        var (tenantId, userId, warehouseId, lines) = Inputs();
        Assert.Throws<DomainException>(() => new Sale(tenantId, "C-1", null, PaymentMethod.Credit, lines, null, userId,
            0.18m, Guid.NewGuid(), "MAIN", warehouseId, DateOnly.FromDateTime(DateTime.Today.AddDays(30))));

        var customer = NewCustomer(tenantId);
        Assert.Throws<DomainException>(() => new Sale(tenantId, "C-2", customer, PaymentMethod.Credit, lines, null, userId,
            0.18m, Guid.NewGuid(), "MAIN", warehouseId));
    }

    [TestMethod]
    public void Partial_and_final_receipts_reduce_balance_to_zero()
    {
        var sale = CreateCreditSale();
        sale.RecordPayments([]);

        sale.RecordReceivablePayment(CreateReceipt(sale, 50m));
        Assert.AreEqual(50m, sale.AmountPaid);
        Assert.AreEqual(68m, sale.BalanceDue);

        sale.RecordReceivablePayment(CreateReceipt(sale, 68m));
        Assert.AreEqual(118m, sale.AmountPaid);
        Assert.AreEqual(0m, sale.BalanceDue);
    }

    [TestMethod]
    public void Receipt_cannot_exceed_open_balance_or_repeat_idempotency_key()
    {
        var sale = CreateCreditSale();
        sale.RecordPayments([]);
        var receiptKey = Guid.NewGuid();
        var firstReceipt = CreateReceipt(sale, 50m, receiptKey);
        sale.RecordReceivablePayment(firstReceipt);

        Assert.Throws<DomainException>(() => sale.RecordReceivablePayment(CreateReceipt(sale, 68.01m)));
        Assert.Throws<DomainException>(() => sale.RecordReceivablePayment(CreateReceipt(sale, 1m, receiptKey)));
    }

    [TestMethod]
    public void Cash_sale_requires_open_cash_session()
    {
        var (tenantId, userId, warehouseId, lines) = Inputs();
        var customer = NewCustomer(tenantId);
        Assert.Throws<DomainException>(() => new Sale(tenantId, "V-1", customer, PaymentMethod.Cash, lines, null, userId,
            0.18m, Guid.NewGuid(), "MAIN", warehouseId));
    }

    [TestMethod]
    public void Cash_receipt_requires_and_preserves_open_session_reference()
    {
        var sale = CreateCreditSale();
        sale.RecordPayments([]);
        var receiptKey = Guid.NewGuid();
        var methodId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var userId = sale.CashierUserId!.Value;
        var customerId = sale.CustomerId!.Value;

        Assert.Throws<DomainException>(() => new CustomerReceipt(sale.TenantId, sale.Id, customerId, userId,
            receiptKey, methodId, 25m, "Efectivo", PaymentMethodType.Cash, null, accountId));

        var cashSessionId = Guid.NewGuid();
        var receipt = new CustomerReceipt(sale.TenantId, sale.Id, customerId, userId, receiptKey, methodId,
            25m, "Efectivo", PaymentMethodType.Cash, null, accountId, cashSessionId);
        sale.RecordReceivablePayment(receipt);
        Assert.AreEqual(cashSessionId, receipt.CashSessionId);
        Assert.AreEqual(93m, sale.BalanceDue);
    }

    private static Sale CreateCreditSale()
    {
        var (tenantId, userId, warehouseId, lines) = Inputs();
        var customer = NewCustomer(tenantId);
        return new Sale(tenantId, "C-TEST", customer, PaymentMethod.Credit, lines, null, userId, 0.18m,
            Guid.NewGuid(), "MAIN", warehouseId, DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
    }

    private static CustomerReceipt CreateReceipt(Sale sale, decimal amount, Guid? idempotencyKey = null) =>
        new(sale.TenantId, sale.Id, sale.CustomerId!.Value, sale.CashierUserId!.Value, idempotencyKey ?? Guid.NewGuid(), Guid.NewGuid(), amount,
            "Transferencia", PaymentMethodType.Transfer, null, Guid.NewGuid());

    private static Customer NewCustomer(Guid tenantId) =>
        new(tenantId, "Cliente", "Prueba", $"{Guid.NewGuid():N}@example.test", "8095550100");

    private static (Guid TenantId, Guid UserId, Guid WarehouseId, SaleLine[] Lines) Inputs()
    {
        var line = new SaleLine(Guid.NewGuid(), "SKU-1", "Artículo de prueba", 100m, 1, 0.18m);
        return (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [line]);
    }
}
