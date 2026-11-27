using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Purchasing;

/// <summary>Records a received-lines request so a retry cannot repeat inventory or accounting effects.</summary>
public sealed class PurchaseReceiptBatch : TenantEntity
{
    private PurchaseReceiptBatch() { }

    public PurchaseReceiptBatch(Guid tenantId, Guid purchaseOrderId, Guid idempotencyKey, string requestFingerprint) : base(tenantId)
    {
        if (purchaseOrderId == Guid.Empty || idempotencyKey == Guid.Empty)
            throw new DomainException("La orden y la clave de idempotencia de la recepción son obligatorias.");
        if (string.IsNullOrWhiteSpace(requestFingerprint) || requestFingerprint.Length > 12000)
            throw new DomainException("El detalle de la recepción no es válido.");
        PurchaseOrderId = purchaseOrderId;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
    }

    public Guid PurchaseOrderId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
}
