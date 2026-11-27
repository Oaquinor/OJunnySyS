using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Domain.Sales;

public enum PaymentMethod { Cash, Card, Transfer, Credit }
public enum SaleStatus { Completed, Voided }

public sealed class Sale : TenantEntity
{
    private Sale() { }
    private readonly List<SaleLine> _lines = [];

    public Sale(Guid tenantId, string number, Customer? customer, PaymentMethod paymentMethod, IEnumerable<SaleLine> lines, Guid cashSessionId, Guid cashierUserId, decimal taxRate = 0.18m, Guid? idempotencyKey = null, string registerCode = "MAIN") : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("El número de venta es obligatorio.");
        if (number.Length > 64) throw new DomainException("El número de venta no puede superar 64 caracteres.");
        if (cashSessionId == Guid.Empty || cashierUserId == Guid.Empty) throw new DomainException("La venta debe quedar asociada a una caja abierta y a un usuario.");
        if (!Enum.IsDefined(paymentMethod)) throw new DomainException("El método de pago no es válido.");
        if (taxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto debe estar entre 0 y 1.");
        RegisterCode = PosRegister.NormalizeCode(registerCode);
        Number = number; CustomerId = customer?.Id; CustomerName = customer?.FullName ?? "Consumidor final";
        CashSessionId = cashSessionId; CashierUserId = cashierUserId;
        PaymentMethod = paymentMethod; TaxRate = taxRate; IdempotencyKey = idempotencyKey;
        _lines.AddRange(lines);
        if (_lines.Count == 0) throw new DomainException("La venta debe contener al menos un producto.");
        Subtotal = _lines.Sum(x => x.LineTotal); TaxAmount = decimal.Round(Subtotal * TaxRate, 2); Total = Subtotal + TaxAmount;
        Status = SaleStatus.Completed;
    }

    public string Number { get; private set; } = "";
    public Guid? CashSessionId { get; private set; }
    public string RegisterCode { get; private set; } = "MAIN";
    public Guid? CashierUserId { get; private set; }
    public Guid? IdempotencyKey { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = "";
    public PaymentMethod PaymentMethod { get; private set; }
    public SaleStatus Status { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;

    public void Void() { if (Status == SaleStatus.Voided) throw new DomainException("La venta ya fue anulada."); Status = SaleStatus.Voided; MarkUpdated(); }
}

public sealed class SaleLine
{
    private SaleLine() { }
    public SaleLine(Guid productId, string sku, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor que cero.");
        if (unitPrice < 0) throw new DomainException("El precio no puede ser negativo.");
        ProductId = productId; Sku = sku; ProductName = productName; UnitPrice = unitPrice; Quantity = quantity;
        LineTotal = decimal.Round(unitPrice * quantity, 2);
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
}
