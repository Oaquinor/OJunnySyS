using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Catalog;

public sealed class Product : TenantEntity
{
    private Product() { }

    public Product(Guid tenantId, string sku, string name, string category, decimal unitPrice, int stockOnHand, string? barcode = null, decimal? taxRateOverride = null, decimal? initialUnitCost = null, Guid? taxProfileId = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("El SKU es obligatorio.");
        if (sku.Trim().Length > 40) throw new DomainException("El SKU no puede superar 40 caracteres.");
        Sku = sku.Trim().ToUpperInvariant();
        UpdateDetails(name, category, unitPrice, barcode, taxRateOverride, taxProfileId);
        if (stockOnHand < 0) throw new DomainException("El inventario no puede ser negativo.");
        if (initialUnitCost is < 0 or > 9999999999.9999m) throw new DomainException("El costo unitario inicial no es válido.");
        UnitCost = decimal.Round(initialUnitCost ?? 0m, 4, MidpointRounding.AwayFromZero);
        UncostedQuantity = initialUnitCost is null ? stockOnHand : 0;
        StockOnHand = stockOnHand; IsActive = true;
    }

    public string Sku { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string? Barcode { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal UnitCost { get; private set; }
    public int UncostedQuantity { get; private set; }
    public decimal? TaxRateOverride { get; private set; }
    public Guid? TaxProfileId { get; private set; }
    public TaxProfile? TaxProfile { get; private set; }
    public int StockOnHand { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void UpdateDetails(string name, string category, decimal unitPrice, string? barcode, decimal? taxRateOverride = null, Guid? taxProfileId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre del producto es obligatorio.");
        category = string.IsNullOrWhiteSpace(category) ? "General" : category;
        if (name.Trim().Length > 160 || category.Trim().Length > 80 || (barcode?.Trim().Length ?? 0) > 64) throw new DomainException("Los datos del producto superan el largo permitido.");
        if (unitPrice < 0) throw new DomainException("El precio no puede ser negativo.");
        if (taxRateOverride is < 0 or > 1) throw new DomainException("El impuesto del producto debe estar entre 0 y 100 por ciento.");
        if (taxProfileId == Guid.Empty) throw new DomainException("Selecciona un perfil de impuesto válido.");
        if (taxProfileId is not null && taxRateOverride is not null) throw new DomainException("Usa un perfil guardado o una tasa personalizada, no ambos.");
        Name = name.Trim(); Category = category.Trim(); UnitPrice = decimal.Round(unitPrice, 2);
        TaxRateOverride = taxRateOverride;
        TaxProfileId = taxProfileId;
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(); MarkChanged();
    }

    public void UpdateCatalog(string sku, string name, string category, decimal unitPrice, string? barcode, decimal? taxRateOverride = null, Guid? taxProfileId = null)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 40) throw new DomainException("El SKU es obligatorio y no puede superar 40 caracteres.");
        Sku = sku.Trim().ToUpperInvariant();
        UpdateDetails(name, category, unitPrice, barcode, taxRateOverride, taxProfileId);
    }

    public void AdjustStock(int delta)
    {
        if (delta < -StockOnHand) throw new DomainException($"Inventario insuficiente para {Name}.");
        if (delta > int.MaxValue - StockOnHand) throw new DomainException($"El inventario supera el límite permitido para {Name}.");
        StockOnHand += delta; MarkChanged();
    }

    public void ApplyPurchaseCost(int receivedUnits, decimal purchaseUnitCost)
    {
        if (receivedUnits <= 0 || purchaseUnitCost < 0)
            throw new DomainException("No se puede actualizar el costo promedio con esos valores.");
        var existingCostedUnits = StockOnHand - UncostedQuantity;
        var totalCostedUnits = checked(existingCostedUnits + receivedUnits);
        UnitCost = decimal.Round(((UnitCost * existingCostedUnits) + (purchaseUnitCost * receivedUnits)) / totalCostedUnits, 4, MidpointRounding.AwayFromZero);
        MarkChanged();
    }

    public InventoryCostSnapshot ConsumeInventory(int quantity)
    {
        if (quantity <= 0) throw new DomainException("La cantidad a vender debe ser mayor que cero.");
        var uncostedSold = Math.Min(UncostedQuantity, quantity);
        UncostedQuantity -= uncostedSold;
        AdjustStock(-quantity);
        return new InventoryCostSnapshot(UnitCost, uncostedSold);
    }

    public void RestockReturnedInventory(int quantity, int costedQuantity, decimal returnedUnitCost)
    {
        if (quantity <= 0 || costedQuantity < 0 || costedQuantity > quantity || returnedUnitCost < 0) throw new DomainException("La cantidad o el costo de la devolución no son válidos.");
        if (quantity > int.MaxValue - StockOnHand) throw new DomainException($"Las existencias superan el límite permitido para {Name}.");
        var currentCosted = StockOnHand - UncostedQuantity;
        if (costedQuantity > 0)
        {
            var totalCosted = checked(currentCosted + costedQuantity);
            UnitCost = decimal.Round(((UnitCost * currentCosted) + (returnedUnitCost * costedQuantity)) / totalCosted, 4, MidpointRounding.AwayFromZero);
        }
        UncostedQuantity = checked(UncostedQuantity + quantity - costedQuantity);
        StockOnHand = checked(StockOnHand + quantity);
        MarkChanged();
    }

    public void Deactivate() { if (!IsActive) return; IsActive = false; MarkChanged(); }
    public void Reactivate() { if (IsActive) return; IsActive = true; MarkChanged(); }
    private void MarkChanged() { Revision++; MarkUpdated(); }
}

public sealed record InventoryCostSnapshot(decimal UnitCost, int UncostedQuantity);
