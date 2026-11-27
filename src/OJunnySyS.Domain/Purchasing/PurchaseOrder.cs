using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Purchasing;

public enum PurchaseStatus { Draft, PartiallyReceived, Received, Cancelled }

public sealed class PurchaseOrder : TenantEntity
{
    private readonly List<PurchaseOrderLine> _lines = [];
    private readonly List<SupplierPayment> _payments = [];
    private PurchaseOrder() { }

    public PurchaseOrder(Guid tenantId, string number, Supplier supplier, IEnumerable<PurchaseOrderLine> lines, string? reference = null, DateOnly? dueDate = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("El número de compra es obligatorio.");
        if (number.Length > 64 || (reference?.Trim().Length ?? 0) > 100) throw new DomainException("El número o referencia de compra supera el largo permitido.");
        if (supplier.TenantId != tenantId || !supplier.IsActive) throw new DomainException("El proveedor no existe o está inactivo.");
        _lines.AddRange(lines);
        if (_lines.Count == 0) throw new DomainException("La compra debe incluir al menos un producto.");
        if (_lines.Count > 200) throw new DomainException("Una orden no puede incluir más de 200 productos distintos.");
        if (_lines.Select(x => x.ProductId).Distinct().Count() != _lines.Count) throw new DomainException("Consolida las líneas duplicadas del mismo producto.");
        Number = number.Trim(); SupplierId = supplier.Id; SupplierName = supplier.Name;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        DueDate = dueDate;
        Status = PurchaseStatus.Draft;
        Total = decimal.Round(_lines.Sum(x => x.LineTotal), 2);
        if (Total > 999999999999.99m) throw new DomainException("El total de la compra supera el máximo permitido.");
    }

    public string Number { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = "";
    public string? Reference { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines;
    public IReadOnlyCollection<SupplierPayment> Payments => _payments;
    public decimal AmountPaid => decimal.Round(_payments.Sum(x => x.Amount), 2);
    public decimal BalanceDue => Status == PurchaseStatus.Cancelled ? 0m : decimal.Round(Total - AmountPaid, 2);

    public PurchaseOrderLine Receive(Guid lineId, int quantity)
    {
        if (Status is PurchaseStatus.Received or PurchaseStatus.Cancelled) throw new DomainException("Esta compra no admite más recepciones.");
        var line = _lines.SingleOrDefault(x => x.Id == lineId) ?? throw new DomainException("La línea no pertenece a esta compra.");
        line.Receive(quantity);
        Status = _lines.All(x => x.IsFullyReceived) ? PurchaseStatus.Received : PurchaseStatus.PartiallyReceived;
        MarkUpdated();
        return line;
    }

    public void Cancel()
    {
        if (Status != PurchaseStatus.Draft || _payments.Count != 0) throw new DomainException("Solo se pueden cancelar compras sin recepciones ni pagos.");
        Status = PurchaseStatus.Cancelled; MarkUpdated();
    }

    public void RecordPayment(SupplierPayment payment)
    {
        if (Status == PurchaseStatus.Cancelled) throw new DomainException("No se puede pagar una compra cancelada.");
        if (payment.TenantId != TenantId || payment.PurchaseOrderId != Id) throw new DomainException("El pago no pertenece a esta compra.");
        if (payment.Amount > BalanceDue) throw new DomainException("El pago supera el saldo pendiente de la compra.");
        if (_payments.Any(x => x.IdempotencyKey == payment.IdempotencyKey)) throw new DomainException("Este pago ya fue registrado.");
        _payments.Add(payment);
        MarkUpdated();
    }
}

public enum SupplierPaymentMethod { Cash, BankTransfer, Card, Check, Other }

public sealed class SupplierPayment : TenantEntity
{
    private SupplierPayment() { }

    public SupplierPayment(Guid tenantId, Guid purchaseOrderId, Guid paidByUserId, Guid idempotencyKey,
        decimal amount, SupplierPaymentMethod method, string? reference, Guid? cashSessionId) : base(tenantId)
    {
        if (purchaseOrderId == Guid.Empty || paidByUserId == Guid.Empty || idempotencyKey == Guid.Empty)
            throw new DomainException("La compra, el usuario y la clave del pago son obligatorios.");
        if (amount is <= 0 or > 999999999999.99m) throw new DomainException("El abono debe ser mayor que cero y estar dentro del límite permitido.");
        if (!Enum.IsDefined(method)) throw new DomainException("El método de pago al proveedor no es válido.");
        if (method == SupplierPaymentMethod.Cash && cashSessionId is null) throw new DomainException("Un pago en efectivo debe quedar asociado a un turno de caja abierto.");
        if (method != SupplierPaymentMethod.Cash && cashSessionId is not null) throw new DomainException("Solo los pagos en efectivo se asocian a la caja.");
        if (reference?.Trim().Length > 100) throw new DomainException("La referencia no puede exceder 100 caracteres.");
        PurchaseOrderId = purchaseOrderId;
        PaidByUserId = paidByUserId;
        IdempotencyKey = idempotencyKey;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Method = method;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        CashSessionId = cashSessionId;
    }

    public Guid PurchaseOrderId { get; private set; }
    public Guid PaidByUserId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public decimal Amount { get; private set; }
    public SupplierPaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public Guid? CashSessionId { get; private set; }
}

public sealed class PurchaseOrderLine
{
    private PurchaseOrderLine() { }
    public PurchaseOrderLine(Guid productId, string sku, string productName, int quantity, decimal unitCost)
    {
        if (productId == Guid.Empty) throw new DomainException("El producto de la compra es obligatorio.");
        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 40 || string.IsNullOrWhiteSpace(productName) || productName.Length > 160) throw new DomainException("Los datos del producto de la compra no son válidos.");
        if (quantity <= 0) throw new DomainException("La cantidad comprada debe ser mayor que cero.");
        if (unitCost is < 0 or > 9999999999.99m) throw new DomainException("El costo unitario debe estar entre 0 y 9,999,999,999.99.");
        ProductId = productId; Sku = sku; ProductName = productName;
        Quantity = quantity; UnitCost = decimal.Round(unitCost, 2);
        LineTotal = decimal.Round(UnitCost * quantity, 2);
        if (LineTotal > 999999999999.99m) throw new DomainException("El importe de la línea supera el máximo permitido.");
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsFullyReceived => ReceivedQuantity == Quantity;

    internal void Receive(int quantity)
    {
        if (quantity <= 0) throw new DomainException("La cantidad recibida debe ser mayor que cero.");
        if (quantity > Quantity - ReceivedQuantity) throw new DomainException("La cantidad recibida supera lo ordenado.");
        ReceivedQuantity += quantity;
    }
}

public enum InventoryMovementType { PurchaseReceipt, Sale, SaleReturn, StockAdjustment }

public sealed class InventoryMovement : TenantEntity
{
    private InventoryMovement() { }
    public InventoryMovement(Guid tenantId, Guid productId, InventoryMovementType type, int quantityDelta, int balanceAfter, string reference) : base(tenantId)
    {
        if (productId == Guid.Empty || quantityDelta == 0 || balanceAfter < 0) throw new DomainException("El movimiento de inventario no es válido.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100) throw new DomainException("La referencia del movimiento no es válida.");
        ProductId = productId; Type = type; QuantityDelta = quantityDelta; BalanceAfter = balanceAfter; Reference = reference;
    }
    public Guid ProductId { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public int QuantityDelta { get; private set; }
    public int BalanceAfter { get; private set; }
    public string Reference { get; private set; } = "";
}
