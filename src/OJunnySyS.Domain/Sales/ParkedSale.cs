using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Pos;

namespace OJunnySyS.Domain.Sales;

public enum ParkedSaleStatus { Parked, Completed, Cancelled }

/// <summary>A persisted in-progress POS cart. It does not reserve stock or create accounting entries.</summary>
public sealed class ParkedSale : TenantEntity
{
    private ParkedSale() { }
    private readonly List<ParkedSaleLine> _lines = [];

    public ParkedSale(Guid tenantId, Guid id, Guid idempotencyKey, string registerCode, Guid cashierUserId,
        Guid? customerId, string customerName, Guid? sellerUserId, string? sellerName,
        IEnumerable<ParkedSaleLine> lines) : base(tenantId)
    {
        if (id == Guid.Empty || idempotencyKey == Guid.Empty || cashierUserId == Guid.Empty)
            throw new DomainException("La clave de guardado y el cajero son obligatorios.");
        Id = id;
        RegisterCode = PosRegister.NormalizeCode(registerCode);
        CashierUserId = cashierUserId;
        if (customerId == Guid.Empty || sellerUserId == Guid.Empty) throw new DomainException("El cliente o vendedor seleccionado no es válido.");
        if (customerId is null && !string.IsNullOrWhiteSpace(customerName) && !string.Equals(customerName.Trim(), "Consumidor final", StringComparison.Ordinal))
            throw new DomainException("No se puede guardar un nombre de cliente sin su referencia.");
        if (sellerUserId is null && !string.IsNullOrWhiteSpace(sellerName)) throw new DomainException("No se puede guardar un vendedor sin su referencia.");
        CustomerId = customerId;
        CustomerName = customerId is null ? "Consumidor final" : NormalizeName(customerName, "cliente", 220);
        SellerUserId = sellerUserId;
        SellerName = sellerUserId is null ? null : NormalizeName(sellerName, "vendedor", 160);
        IdempotencyKey = idempotencyKey;
        _lines.AddRange(lines ?? throw new DomainException("Agrega productos antes de guardar la venta en espera."));
        if (_lines.Count is < 1 or > 200 || _lines.Any(x => x.TenantId != tenantId || x.ParkedSaleId != Id))
            throw new DomainException("La venta en espera debe contener entre uno y 200 artículos propios de esta empresa.");
        if (_lines.Select(x => x.ProductId).Distinct().Count() != _lines.Count)
            throw new DomainException("Cada producto debe aparecer una sola vez en la venta en espera.");
        Status = ParkedSaleStatus.Parked;
    }

    public string RegisterCode { get; private set; } = "MAIN";
    public Guid CashierUserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = "Consumidor final";
    public Guid? SellerUserId { get; private set; }
    public string? SellerName { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public ParkedSaleStatus Status { get; private set; }
    public Guid? ConvertedSaleId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public long Revision { get; private set; }
    public IReadOnlyCollection<ParkedSaleLine> Lines => _lines;

    public void Complete(Guid saleId)
    {
        if (saleId == Guid.Empty) throw new DomainException("La venta convertida no es válida.");
        if (Status != ParkedSaleStatus.Parked) throw new DomainException("Esta venta en espera ya no está disponible.");
        Status = ParkedSaleStatus.Completed;
        ConvertedSaleId = saleId;
        AdvanceRevision();
    }

    public void Cancel(Guid actorUserId, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (actorUserId == Guid.Empty) throw new DomainException("El usuario que cancela es obligatorio.");
        if (Status != ParkedSaleStatus.Parked) throw new DomainException("Esta venta en espera ya no se puede cancelar.");
        Status = ParkedSaleStatus.Cancelled;
        CancelledByUserId = actorUserId;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        AdvanceRevision();
    }

    private void AdvanceRevision()
    {
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("La venta en espera cambió en otra terminal. Actualiza la lista antes de continuar.");
    }

    private static string NormalizeName(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
            throw new DomainException($"El nombre del {field} no es válido.");
        return normalized;
    }
}

public sealed class ParkedSaleLine : TenantEntity
{
    private ParkedSaleLine() { }

    public ParkedSaleLine(Guid tenantId, Guid id, Guid parkedSaleId, Guid productId, string sku, string productName,
        int quantity, decimal discountPercent) : base(tenantId)
    {
        if (id == Guid.Empty || parkedSaleId == Guid.Empty || productId == Guid.Empty || quantity <= 0)
            throw new DomainException("El artículo guardado y su cantidad no son válidos.");
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 40 || string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 160)
            throw new DomainException("El artículo guardado no tiene una referencia válida.");
        if (discountPercent is < 0 or > 100) throw new DomainException("El descuento debe estar entre 0 y 100 por ciento.");
        Id = id;
        ParkedSaleId = parkedSaleId;
        ProductId = productId;
        Sku = sku.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        DiscountPercent = decimal.Round(discountPercent, 2, MidpointRounding.AwayFromZero);
    }

    public Guid ParkedSaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal DiscountPercent { get; private set; }
}
