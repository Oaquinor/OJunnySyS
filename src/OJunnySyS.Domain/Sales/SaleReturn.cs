using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Cash;

namespace OJunnySyS.Domain.Sales;

/// <summary>Immutable credit note and optional refund issued against a completed sale.</summary>
public sealed class SaleReturn : TenantEntity
{
    private readonly List<SaleReturnLine> _lines = [];
    private SaleReturn() { }

    public SaleReturn(Guid tenantId, Guid id, Guid saleId, string number, Guid idempotencyKey, IEnumerable<SaleReturnLine> lines,
        decimal receivableCredit, decimal refundAmount, Guid? paymentMethodId, string? paymentMethodName,
        Guid? ledgerAccountId, PaymentMethodType? paymentMethodType, Guid? cashSessionId) : base(tenantId)
    {
        if (id == Guid.Empty || saleId == Guid.Empty || idempotencyKey == Guid.Empty) throw new DomainException("La venta, devolución y clave son obligatorias.");
        if (string.IsNullOrWhiteSpace(number) || number.Length > 64) throw new DomainException("El número de nota de crédito no es válido.");
        Id = id;
        _lines.AddRange(lines ?? throw new DomainException("La devolución debe incluir artículos."));
        if (_lines.Count is < 1 or > 200) throw new DomainException("La devolución debe incluir entre uno y 200 renglones.");
        if (_lines.Any(x => x.TenantId != tenantId || x.SaleReturnId != Id)) throw new DomainException("Los renglones no pertenecen a esta devolución.");
        Subtotal = decimal.Round(_lines.Sum(x => x.LineTotal), 2, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(_lines.Sum(x => x.TaxAmount), 2, MidpointRounding.AwayFromZero);
        Total = decimal.Round(Subtotal + TaxAmount, 2, MidpointRounding.AwayFromZero);
        if (Total < 0) throw new DomainException("El total de la devolución no puede ser negativo.");
        if (receivableCredit < 0 || refundAmount < 0 || decimal.Round(receivableCredit + refundAmount, 2) != Total)
            throw new DomainException("La nota de crédito y el reembolso deben distribuir el total de la devolución.");
        if (refundAmount > 0 && (paymentMethodId is null || ledgerAccountId is null || paymentMethodType is null || string.IsNullOrWhiteSpace(paymentMethodName)))
            throw new DomainException("Selecciona el medio de pago para el reembolso.");
        if (paymentMethodType is { } methodType && !Enum.IsDefined(methodType)) throw new DomainException("El medio de reembolso no es válido.");
        if (paymentMethodId == Guid.Empty || ledgerAccountId == Guid.Empty) throw new DomainException("La forma de pago del reembolso no es válida.");
        if (refundAmount == 0 && (paymentMethodId is not null || ledgerAccountId is not null || paymentMethodType is not null || cashSessionId is not null))
            throw new DomainException("No se debe registrar un pago cuando toda la nota reduce la cuenta por cobrar.");
        if (paymentMethodType == PaymentMethodType.Cash && refundAmount > 0 && cashSessionId is null)
            throw new DomainException("Abre la caja para devolver efectivo.");
        SaleId = saleId; Number = number; IdempotencyKey = idempotencyKey; ReceivableCredit = receivableCredit; RefundAmount = refundAmount;
        PaymentMethodId = paymentMethodId; PaymentMethodName = paymentMethodName; LedgerAccountId = ledgerAccountId;
        RefundMethodType = paymentMethodType; CashSessionId = cashSessionId;
    }

    public Guid SaleId { get; private set; }
    public string Number { get; private set; } = "";
    public Guid IdempotencyKey { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public decimal ReceivableCredit { get; private set; }
    public decimal RefundAmount { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public string? PaymentMethodName { get; private set; }
    public Guid? LedgerAccountId { get; private set; }
    public PaymentMethodType? RefundMethodType { get; private set; }
    public Guid? CashSessionId { get; private set; }
    public IReadOnlyCollection<SaleReturnLine> Lines => _lines;
}

public sealed class SaleReturnLine : TenantEntity
{
    private SaleReturnLine() { }
    public SaleReturnLine(Guid tenantId, Guid id, Guid saleReturnId, Guid saleLineId, Guid productId, string sku, string productName,
        int quantity, int costedQuantity, decimal unitPrice, decimal taxRate, decimal unitCost, decimal discountPercent = 0m,
        decimal? lineTotalSnapshot = null, decimal? taxAmountSnapshot = null) : base(tenantId)
    {
        if (saleReturnId == Guid.Empty || saleLineId == Guid.Empty || productId == Guid.Empty || quantity <= 0)
            throw new DomainException("El artículo y la cantidad devuelta no son válidos.");
        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 40 || string.IsNullOrWhiteSpace(productName) || productName.Length > 160 ||
            unitPrice < 0 || unitCost < 0 || taxRate is < 0 or > 1 || discountPercent is < 0 or > 100)
            throw new DomainException("Los datos históricos del artículo devuelto no son válidos.");
        if (id == Guid.Empty || costedQuantity < 0 || costedQuantity > quantity) throw new DomainException("La cantidad devuelta con costo no es válida.");
        Id = id; SaleReturnId = saleReturnId; SaleLineId = saleLineId; ProductId = productId;
        Sku = sku; ProductName = productName; Quantity = quantity; CostedQuantity = costedQuantity; UnitPrice = unitPrice; TaxRate = taxRate; UnitCost = unitCost;
        DiscountPercent = decimal.Round(discountPercent, 2, MidpointRounding.AwayFromZero);
        var grossAmount = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        LineTotal = lineTotalSnapshot ?? grossAmount - decimal.Round(grossAmount * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        TaxAmount = taxAmountSnapshot ?? decimal.Round(LineTotal * taxRate, 2, MidpointRounding.AwayFromZero);
        if (LineTotal < 0 || LineTotal > grossAmount || TaxAmount < 0 || TaxAmount > LineTotal)
            throw new DomainException("Los importes históricos de la devolución no son válidos.");
        DiscountAmount = grossAmount - LineTotal;
        CostTotal = decimal.Round(unitCost * costedQuantity, 2, MidpointRounding.AwayFromZero);
    }
    public Guid SaleReturnId { get; private set; }
    public Guid SaleLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal UnitCost { get; private set; }
    public int CostedQuantity { get; private set; }
    public int UncostedQuantity => Quantity - CostedQuantity;
    public decimal LineTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal CostTotal { get; private set; }
}
