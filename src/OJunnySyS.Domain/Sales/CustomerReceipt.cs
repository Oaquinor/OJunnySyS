using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Cash;

namespace OJunnySyS.Domain.Sales;

/// <summary>An immutable customer collection against one credit sale.</summary>
public sealed class CustomerReceipt : TenantEntity
{
    private CustomerReceipt() { }

    public CustomerReceipt(Guid tenantId, Guid saleId, Guid customerId, Guid receivedByUserId, Guid idempotencyKey, Guid paymentMethodId,
        decimal amount, string methodName, PaymentMethodType methodType, string? reference, Guid ledgerAccountId,
        Guid? cashSessionId = null) : base(tenantId)
    {
        if (saleId == Guid.Empty || customerId == Guid.Empty || receivedByUserId == Guid.Empty || idempotencyKey == Guid.Empty || paymentMethodId == Guid.Empty)
            throw new DomainException("La venta, cliente, usuario y clave de idempotencia son obligatorios.");
        if (amount is <= 0 or > 999999999999.99m || amount != decimal.Round(amount, 2)) throw new DomainException("El abono debe ser positivo y tener hasta dos decimales.");
        if (string.IsNullOrWhiteSpace(methodName) || methodName.Trim().Length > 80 || !Enum.IsDefined(methodType)) throw new DomainException("El medio de cobro no es válido.");
        if (ledgerAccountId == Guid.Empty) throw new DomainException("La cuenta contable del cobro es obligatoria.");
        if (reference?.Trim().Length > 120) throw new DomainException("La referencia no puede superar 120 caracteres.");
        if (methodType == PaymentMethodType.Cash && cashSessionId is null) throw new DomainException("Un cobro en efectivo debe quedar asociado a una caja abierta.");
        if (methodType != PaymentMethodType.Cash && cashSessionId is not null) throw new DomainException("Solo el efectivo se asocia a la caja.");
        SaleId = saleId; CustomerId = customerId; ReceivedByUserId = receivedByUserId; IdempotencyKey = idempotencyKey;
        Amount = amount; MethodName = methodName.Trim(); MethodType = methodType; Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        LedgerAccountId = ledgerAccountId; CashSessionId = cashSessionId; PaymentMethodId = paymentMethodId;
    }

    public Guid SaleId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ReceivedByUserId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public decimal Amount { get; private set; }
    public string MethodName { get; private set; } = "";
    public PaymentMethodType MethodType { get; private set; }
    public string? Reference { get; private set; }
    public Guid LedgerAccountId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public Guid? CashSessionId { get; private set; }
}

