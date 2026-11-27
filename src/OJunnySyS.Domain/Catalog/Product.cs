using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Catalog;

public sealed class Product : TenantEntity
{
    private Product() { }

    public Product(Guid tenantId, string sku, string name, string category, decimal unitPrice, int stockOnHand, string? barcode = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("El SKU es obligatorio.");
        if (sku.Trim().Length > 40) throw new DomainException("El SKU no puede superar 40 caracteres.");
        Sku = sku.Trim().ToUpperInvariant();
        UpdateDetails(name, category, unitPrice, barcode);
        if (stockOnHand < 0) throw new DomainException("El inventario no puede ser negativo.");
        StockOnHand = stockOnHand; IsActive = true;
    }

    public string Sku { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string? Barcode { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockOnHand { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void UpdateDetails(string name, string category, decimal unitPrice, string? barcode)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre del producto es obligatorio.");
        category = string.IsNullOrWhiteSpace(category) ? "General" : category;
        if (name.Trim().Length > 160 || category.Trim().Length > 80 || (barcode?.Trim().Length ?? 0) > 64) throw new DomainException("Los datos del producto superan el largo permitido.");
        if (unitPrice < 0) throw new DomainException("El precio no puede ser negativo.");
        Name = name.Trim(); Category = category.Trim(); UnitPrice = decimal.Round(unitPrice, 2);
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(); MarkChanged();
    }

    public void UpdateCatalog(string sku, string name, string category, decimal unitPrice, string? barcode)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 40) throw new DomainException("El SKU es obligatorio y no puede superar 40 caracteres.");
        Sku = sku.Trim().ToUpperInvariant();
        UpdateDetails(name, category, unitPrice, barcode);
    }

    public void AdjustStock(int delta)
    {
        if (delta < -StockOnHand) throw new DomainException($"Inventario insuficiente para {Name}.");
        if (delta > int.MaxValue - StockOnHand) throw new DomainException($"El inventario supera el límite permitido para {Name}.");
        StockOnHand += delta; MarkChanged();
    }

    public void Deactivate() { if (!IsActive) return; IsActive = false; MarkChanged(); }
    public void Reactivate() { if (IsActive) return; IsActive = true; MarkChanged(); }
    private void MarkChanged() { Revision++; MarkUpdated(); }
}
