using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Inventory;

public sealed class InventoryAdjustment : TenantEntity
{
    private InventoryAdjustment() { }

    public InventoryAdjustment(Guid tenantId, string number, Guid idempotencyKey, string requestFingerprint,
        Guid productId, string productSku, string productName, Guid warehouseId, string warehouseName,
        Guid actorUserId, string actorName, int quantityDelta, int balanceBefore, int balanceAfter,
        decimal unitCost, decimal valuationAmount, string reason, Guid counterAccountId,
        string counterAccountCode, string counterAccountName, string? journalEntryNumber, int? countedQuantity = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length > 40 || idempotencyKey == Guid.Empty ||
            requestFingerprint?.Length != 64 || productId == Guid.Empty || warehouseId == Guid.Empty || actorUserId == Guid.Empty ||
            (quantityDelta == 0 && countedQuantity is null) || balanceBefore < 0 || balanceAfter < 0 || (long)balanceBefore + quantityDelta != balanceAfter ||
            (countedQuantity is not null && (countedQuantity < 0 || countedQuantity != balanceAfter)))
            throw new DomainException("Los datos del ajuste de inventario no son válidos.");
        if (string.IsNullOrWhiteSpace(productSku) || productSku.Length > 40 || string.IsNullOrWhiteSpace(productName) || productName.Length > 160 ||
            string.IsNullOrWhiteSpace(warehouseName) || warehouseName.Length > 120 || string.IsNullOrWhiteSpace(actorName) || actorName.Length > 160)
            throw new DomainException("Los datos históricos del ajuste no son válidos.");
        if (unitCost < 0 || valuationAmount < 0 || decimal.Round(valuationAmount, 2) != valuationAmount)
            throw new DomainException("La valoración del ajuste no es válida.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 240)
            throw new DomainException("Explica el motivo del ajuste con entre 3 y 240 caracteres.");
        if (counterAccountId == Guid.Empty || string.IsNullOrWhiteSpace(counterAccountCode) || counterAccountCode.Length > 20 ||
            string.IsNullOrWhiteSpace(counterAccountName) || counterAccountName.Length > 120)
            throw new DomainException("La cuenta de contrapartida del ajuste no es válida.");
        if (journalEntryNumber?.Length > 40) throw new DomainException("El número del asiento contable no es válido.");

        Number = number.Trim(); IdempotencyKey = idempotencyKey; RequestFingerprint = requestFingerprint;
        ProductId = productId; ProductSku = productSku; ProductName = productName;
        WarehouseId = warehouseId; WarehouseName = warehouseName;
        ActorUserId = actorUserId; ActorName = actorName;
        QuantityDelta = quantityDelta; BalanceBefore = balanceBefore; BalanceAfter = balanceAfter;
        CountedQuantity = countedQuantity;
        UnitCost = decimal.Round(unitCost, 4, MidpointRounding.AwayFromZero); ValuationAmount = valuationAmount;
        Reason = reason.Trim(); CounterAccountId = counterAccountId;
        CounterAccountCode = counterAccountCode; CounterAccountName = counterAccountName;
        JournalEntryNumber = journalEntryNumber;
    }

    public string Number { get; private set; } = string.Empty;
    public Guid IdempotencyKey { get; private set; }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public int QuantityDelta { get; private set; }
    public int BalanceBefore { get; private set; }
    public int BalanceAfter { get; private set; }
    public int? CountedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal ValuationAmount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CounterAccountId { get; private set; }
    public string CounterAccountCode { get; private set; } = string.Empty;
    public string CounterAccountName { get; private set; } = string.Empty;
    public string? JournalEntryNumber { get; private set; }
}
