using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Inventory;

namespace OJunnySyS.Domain.Sales;

public enum PaymentMethod { Cash, Card, Transfer, Credit, Other }
public enum SaleStatus { Completed, Voided }

public sealed class Sale : TenantEntity
{
    private Sale() { }
    private readonly List<SaleLine> _lines = [];
    private readonly List<SalePayment> _payments = [];

    public Sale(Guid tenantId, string number, Customer? customer, PaymentMethod paymentMethod, IEnumerable<SaleLine> lines, Guid cashSessionId, Guid cashierUserId, decimal taxRate = 0.18m, Guid? idempotencyKey = null, string registerCode = "MAIN", Guid warehouseId = default) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("El número de venta es obligatorio.");
        if (number.Length > 64) throw new DomainException("El número de venta no puede superar 64 caracteres.");
        if (cashSessionId == Guid.Empty || cashierUserId == Guid.Empty) throw new DomainException("La venta debe quedar asociada a una caja abierta y a un usuario.");
        if (!Enum.IsDefined(paymentMethod)) throw new DomainException("El método de pago no es válido.");
        if (taxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto debe estar entre 0 y 1.");
        RegisterCode = PosRegister.NormalizeCode(registerCode);
        if (warehouseId == Guid.Empty) throw new DomainException("El almacén de la venta es obligatorio.");
        WarehouseId = warehouseId;
        Number = number; CustomerId = customer?.Id; CustomerName = customer?.FullName ?? "Consumidor final";
        CashSessionId = cashSessionId; CashierUserId = cashierUserId;
        PaymentMethod = paymentMethod; TaxRate = taxRate; IdempotencyKey = idempotencyKey;
        _lines.AddRange(lines);
        if (_lines.Count == 0) throw new DomainException("La venta debe contener al menos un producto.");
        Subtotal = _lines.Sum(x => x.LineTotal); TaxAmount = _lines.Sum(x => x.TaxAmount); Total = Subtotal + TaxAmount;
        Status = SaleStatus.Completed;
    }

    public string Number { get; private set; } = "";
    public Guid? CashSessionId { get; private set; }
    public string RegisterCode { get; private set; } = "MAIN";
    public Guid WarehouseId { get; private set; }
    public Guid? CashierUserId { get; private set; }
    public Guid? SellerUserId { get; private set; }
    public string? SellerName { get; private set; }
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
    public IReadOnlyCollection<SalePayment> Payments => _payments;

    public void AssignSeller(Guid? sellerUserId, string? sellerName)
    {
        if (SellerUserId is not null) throw new DomainException("El vendedor de esta venta ya quedó registrado.");
        if (sellerUserId is null)
        {
            if (!string.IsNullOrWhiteSpace(sellerName)) throw new DomainException("No se puede guardar un nombre de vendedor sin seleccionar su cuenta.");
            return;
        }
        if (sellerUserId == Guid.Empty || string.IsNullOrWhiteSpace(sellerName) || sellerName.Trim().Length > 160)
            throw new DomainException("El vendedor seleccionado no es válido.");
        SellerUserId = sellerUserId;
        SellerName = sellerName.Trim();
    }

    public void RecordPayments(IEnumerable<PaymentAllocation> allocations)
    {
        if (_payments.Count != 0) throw new DomainException("Los pagos de esta venta ya quedaron registrados.");
        var items = allocations?.ToArray() ?? throw new DomainException("Debes seleccionar al menos un medio de pago.");
        if (items.Length is < 1 or > 10) throw new DomainException("Una venta debe tener entre uno y diez medios de pago.");
        if (items.Any(x => x.PaymentMethodId == Guid.Empty) || items.Select(x => x.PaymentMethodId).Distinct().Count() != items.Length)
            throw new DomainException("Cada medio de pago puede agregarse una sola vez a la venta.");
        if (items.Any(x => x.Type == PaymentMethodType.Other && x.Amount > 0 && string.IsNullOrWhiteSpace(x.MethodName)))
            throw new DomainException("Indica el nombre de cada medio de pago.");
        if (items.Sum(x => x.Amount) != Total)
            throw new DomainException("La suma de los medios de pago debe coincidir exactamente con el total de la venta.");
        foreach (var item in items) _payments.Add(new SalePayment(TenantId, Id, item.PaymentMethodId, item.MethodName, item.Type, item.Amount, item.Reference));
        PaymentMethod = items.Length > 1 ? PaymentMethod.Other : items[0].Type switch
        {
            PaymentMethodType.Cash => PaymentMethod.Cash,
            PaymentMethodType.Card => PaymentMethod.Card,
            PaymentMethodType.Transfer => PaymentMethod.Transfer,
            _ => PaymentMethod.Other
        };
    }

    public void Void() { if (Status == SaleStatus.Voided) throw new DomainException("La venta ya fue anulada."); Status = SaleStatus.Voided; MarkUpdated(); }
}

public sealed class SaleLine
{
    private SaleLine() { }
    public SaleLine(Guid productId, string sku, string productName, decimal unitPrice, int quantity, decimal taxRate = 0m)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor que cero.");
        if (unitPrice < 0) throw new DomainException("El precio no puede ser negativo.");
        if (taxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto del artículo debe estar entre 0 y 100 por ciento.");
        ProductId = productId; Sku = sku; ProductName = productName; UnitPrice = unitPrice; Quantity = quantity;
        LineTotal = decimal.Round(unitPrice * quantity, 2); TaxRate = taxRate;
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount => decimal.Round(LineTotal * TaxRate, 2);
}
