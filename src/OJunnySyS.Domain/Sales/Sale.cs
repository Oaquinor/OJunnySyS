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
    private readonly List<CustomerReceipt> _receipts = [];
    private readonly List<SaleReturn> _returns = [];

    public Sale(Guid tenantId, string number, Customer? customer, PaymentMethod paymentMethod, IEnumerable<SaleLine> lines, Guid? cashSessionId, Guid cashierUserId, decimal taxRate = 0.18m, Guid? idempotencyKey = null, string registerCode = "MAIN", Guid warehouseId = default, DateOnly? dueDate = null, Guid? salesQuoteId = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("El número de venta es obligatorio.");
        if (number.Length > 64) throw new DomainException("El número de venta no puede superar 64 caracteres.");
        if (cashierUserId == Guid.Empty) throw new DomainException("La venta debe quedar asociada a un usuario.");
        if (paymentMethod != PaymentMethod.Credit && (cashSessionId is null || cashSessionId == Guid.Empty))
            throw new DomainException("Las ventas con cobro inmediato requieren una caja abierta.");
        if (customer is not null && (customer.TenantId != tenantId || !customer.IsActive)) throw new DomainException("El cliente no pertenece a la empresa o está inactivo.");
        if (paymentMethod == PaymentMethod.Credit && (customer is null || dueDate is null))
            throw new DomainException("Las ventas a crédito requieren un cliente y una fecha de vencimiento.");
        if (!Enum.IsDefined(paymentMethod)) throw new DomainException("El método de pago no es válido.");
        if (taxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto debe estar entre 0 y 1.");
        RegisterCode = PosRegister.NormalizeCode(registerCode);
        if (warehouseId == Guid.Empty) throw new DomainException("El almacén de la venta es obligatorio.");
        WarehouseId = warehouseId;
        SalesQuoteId = salesQuoteId;
        Number = number; CustomerId = customer?.Id; CustomerName = customer?.FullName ?? "Consumidor final";
        CashSessionId = cashSessionId; CashierUserId = cashierUserId; DueDate = dueDate;
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
    public Guid? SalesQuoteId { get; private set; }
    public Guid? CashierUserId { get; private set; }
    public Guid? SellerUserId { get; private set; }
    public string? SellerName { get; private set; }
    public Guid? IdempotencyKey { get; private set; }
    public Guid? CustomerId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string CustomerName { get; private set; } = "";
    public PaymentMethod PaymentMethod { get; private set; }
    public SaleStatus Status { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;
    public IReadOnlyCollection<SalePayment> Payments => _payments;
    public IReadOnlyCollection<CustomerReceipt> Receipts => _receipts;
    public IReadOnlyCollection<SaleReturn> Returns => _returns;
    public decimal AmountPaid => decimal.Round(_payments.Sum(x => x.Amount) + _receipts.Sum(x => x.Amount), 2);
    public decimal ReturnedTotal => decimal.Round(_returns.Sum(x => x.Total), 2);
    public decimal BalanceDue => Status == SaleStatus.Voided ? 0m : Math.Max(0m, decimal.Round(Total - AmountPaid - ReturnedTotal, 2));

    public void RecordReturn(SaleReturn saleReturn)
    {
        if (Status == SaleStatus.Voided) throw new DomainException("No se puede devolver una venta anulada.");
        if (saleReturn.TenantId != TenantId || saleReturn.SaleId != Id) throw new DomainException("La devolución no pertenece a esta venta.");
        if (_returns.Any(x => x.IdempotencyKey == saleReturn.IdempotencyKey)) throw new DomainException("Esta devolución ya fue registrada.");
        if (saleReturn.Total > Total - ReturnedTotal) throw new DomainException("El importe devuelto supera el total de la venta disponible.");
        foreach (var returnedLine in saleReturn.Lines)
        {
            var original = _lines.SingleOrDefault(x => x.Id == returnedLine.SaleLineId)
                ?? throw new DomainException("El renglón devuelto no pertenece a esta venta.");
            if (returnedLine.ProductId != original.ProductId || returnedLine.UnitPrice != original.UnitPrice || returnedLine.TaxRate != original.TaxRate || returnedLine.UnitCost != original.UnitCost)
                throw new DomainException("La devolución debe conservar los datos históricos del artículo vendido.");
            var priorQuantity = _returns.SelectMany(x => x.Lines).Where(x => x.SaleLineId == original.Id).Sum(x => x.Quantity);
            if (returnedLine.Quantity > original.Quantity - priorQuantity) throw new DomainException("La cantidad devuelta supera la cantidad vendida disponible.");
        }
        var expectedReceivableCredit = Math.Min(saleReturn.Total, BalanceDue);
        if (saleReturn.ReceivableCredit != expectedReceivableCredit || saleReturn.RefundAmount != saleReturn.Total - expectedReceivableCredit)
            throw new DomainException("El importe debe aplicarse primero al saldo pendiente y reembolsarse solo después de cubrirlo.");
        _returns.Add(saleReturn);
        MarkUpdated();
    }

    public void RecordReceivablePayment(CustomerReceipt receipt)
    {
        if (PaymentMethod != PaymentMethod.Credit || CustomerId is null) throw new DomainException("Esta venta no tiene una cuenta por cobrar.");
        if (Status == SaleStatus.Voided) throw new DomainException("No se puede cobrar una venta anulada.");
        if (receipt.TenantId != TenantId || receipt.SaleId != Id) throw new DomainException("El recibo no pertenece a esta venta.");
        if (receipt.Amount > BalanceDue) throw new DomainException("El abono supera el saldo pendiente.");
        if (_receipts.Any(x => x.IdempotencyKey == receipt.IdempotencyKey)) throw new DomainException("Este recibo ya fue registrado.");
        _receipts.Add(receipt); MarkUpdated();
    }

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
        if (PaymentMethod == PaymentMethod.Credit)
        {
            if (CustomerId is null || DueDate is null) throw new DomainException("Las ventas a crédito requieren un cliente y una fecha de vencimiento.");
            if (items.Length != 0) throw new DomainException("La venta a crédito no puede incluir cobros iniciales en este flujo.");
            return;
        }
        if (items.Length is < 1 or > 10) throw new DomainException("Una venta debe tener entre uno y diez medios de pago.");
        if (items.Any(x => x.PaymentMethodId == Guid.Empty) || items.Select(x => x.PaymentMethodId).Distinct().Count() != items.Length)
            throw new DomainException("Cada medio de pago puede agregarse una sola vez a la venta.");
        if (items.Any(x => x.Type == PaymentMethodType.Other && x.Amount > 0 && string.IsNullOrWhiteSpace(x.MethodName)))
            throw new DomainException("Indica el nombre de cada medio de pago.");
        if (items.Sum(x => x.Amount) != Total)
            throw new DomainException("La suma de los medios de pago debe coincidir exactamente con el total de la venta.");
        foreach (var item in items) _payments.Add(new SalePayment(TenantId, Id, item.PaymentMethodId, item.MethodName,
            item.Type, item.Amount, item.Reference, item.LedgerAccountId));
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
    public SaleLine(Guid productId, string sku, string productName, decimal unitPrice, int quantity, decimal taxRate = 0m, decimal unitCost = 0m, int uncostedQuantity = 0)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor que cero.");
        if (unitPrice < 0) throw new DomainException("El precio no puede ser negativo.");
        if (taxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto del artículo debe estar entre 0 y 100 por ciento.");
        if (unitCost is < 0 or > 9999999999.9999m) throw new DomainException("El costo unitario del artículo no es válido.");
        if (uncostedQuantity < 0 || uncostedQuantity > quantity) throw new DomainException("La cantidad sin costo del artículo no es válida.");
        ProductId = productId; Sku = sku; ProductName = productName; UnitPrice = unitPrice; Quantity = quantity;
        UnitCost = decimal.Round(unitCost, 4, MidpointRounding.AwayFromZero);
        UncostedQuantity = uncostedQuantity;
        LineTotal = decimal.Round(unitPrice * quantity, 2); TaxRate = taxRate;
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public decimal UnitCost { get; private set; }
    public int UncostedQuantity { get; private set; }
    public int CostedQuantity => Quantity - UncostedQuantity;
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount => decimal.Round(LineTotal * TaxRate, 2);
    public decimal CostTotal => decimal.Round(UnitCost * CostedQuantity, 2, MidpointRounding.AwayFromZero);
}
