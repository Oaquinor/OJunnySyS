using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;

namespace OJunnySyS.Domain.Sales;

public enum SalesQuoteStatus { Draft, Sent, Accepted, Rejected, Converted, Cancelled }

/// <summary>A non-fiscal offer with immutable customer, price and tax snapshots until converted.</summary>
public sealed class SalesQuote : TenantEntity
{
    private SalesQuote() { }
    private readonly List<SalesQuoteLine> _lines = [];

    public SalesQuote(Guid tenantId, string number, Customer customer, DateOnly validUntil, string? notes,
        IEnumerable<SalesQuoteLine> lines) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length > 40) throw new DomainException("El número de cotización no es válido.");
        if (customer is null || customer.TenantId != tenantId || !customer.IsActive) throw new DomainException("Selecciona un cliente activo de esta empresa.");
        Number = number.Trim(); CustomerId = customer.Id; CustomerName = customer.FullName;
        ValidUntil = validUntil; Notes = NormalizeNotes(notes);
        ReplaceLines(lines);
        Status = SalesQuoteStatus.Draft;
    }

    public string Number { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public DateOnly ValidUntil { get; private set; }
    public string? Notes { get; private set; }
    public SalesQuoteStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public Guid? ConvertedSaleId { get; private set; }
    public long Revision { get; private set; }
    public IReadOnlyCollection<SalesQuoteLine> Lines => _lines;

    public void UpdateDraft(Customer customer, DateOnly validUntil, string? notes, IEnumerable<SalesQuoteLine> lines, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (Status != SalesQuoteStatus.Draft) throw new DomainException("Solo se puede editar una cotización en borrador.");
        if (customer is null || customer.TenantId != TenantId || !customer.IsActive) throw new DomainException("Selecciona un cliente activo de esta empresa.");
        CustomerId = customer.Id; CustomerName = customer.FullName; ValidUntil = validUntil; Notes = NormalizeNotes(notes);
        ReplaceLines(lines); AdvanceRevision();
    }

    public void MarkSent(long expectedRevision, DateOnly today)
    {
        EnsureRevision(expectedRevision);
        if (Status != SalesQuoteStatus.Draft) throw new DomainException("Solo se puede enviar una cotización en borrador.");
        if (ValidUntil < today) throw new DomainException("La vigencia debe ser hoy o una fecha futura antes de enviarla.");
        Status = SalesQuoteStatus.Sent; AdvanceRevision();
    }

    public void Accept(long expectedRevision, DateOnly today)
    {
        EnsureRevision(expectedRevision);
        if (Status != SalesQuoteStatus.Sent) throw new DomainException("Solo se puede aceptar una cotización enviada.");
        if (ValidUntil < today) throw new DomainException("La cotización venció. Crea una nueva versión para conservar los precios actuales.");
        Status = SalesQuoteStatus.Accepted; AdvanceRevision();
    }

    public void Reject(long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (Status != SalesQuoteStatus.Sent) throw new DomainException("Solo se puede rechazar una cotización enviada.");
        Status = SalesQuoteStatus.Rejected; AdvanceRevision();
    }

    public void Cancel(long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (Status is SalesQuoteStatus.Converted or SalesQuoteStatus.Cancelled or SalesQuoteStatus.Rejected)
            throw new DomainException("Esta cotización ya no se puede cancelar.");
        Status = SalesQuoteStatus.Cancelled; AdvanceRevision();
    }

    public void MarkConverted(Guid saleId)
    {
        if (Status != SalesQuoteStatus.Accepted || saleId == Guid.Empty || ConvertedSaleId is not null)
            throw new DomainException("La cotización no está disponible para convertirse en venta.");
        Status = SalesQuoteStatus.Converted; ConvertedSaleId = saleId; AdvanceRevision();
    }

    private void ReplaceLines(IEnumerable<SalesQuoteLine> lines)
    {
        var items = lines?.ToArray() ?? throw new DomainException("Agrega al menos un producto a la cotización.");
        if (items.Length is < 1 or > 100) throw new DomainException("La cotización debe tener entre 1 y 100 líneas.");
        if (items.Any(x => x.TenantId != TenantId)) throw new DomainException("Una línea de cotización pertenece a otra empresa.");
        if (items.GroupBy(x => x.ProductId).Any(x => x.Count() > 1)) throw new DomainException("Combina las cantidades de cada producto en una sola línea.");
        _lines.Clear(); _lines.AddRange(items);
        Subtotal = decimal.Round(items.Sum(x => x.LineTotal), 2, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round(items.Sum(x => x.TaxAmount), 2, MidpointRounding.AwayFromZero);
        Total = Subtotal + TaxAmount;
    }

    private static string? NormalizeNotes(string? notes)
    {
        var value = notes?.Trim();
        if (value?.Length > 1000) throw new DomainException("Las notas no pueden superar 1,000 caracteres.");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void EnsureRevision(long expected)
    {
        if (Revision != expected) throw new ConcurrencyConflictException("La cotización cambió en otra sesión. Recarga antes de continuar.");
    }

    private void AdvanceRevision() { Revision = checked(Revision + 1); MarkUpdated(); }
}

public sealed class SalesQuoteLine : TenantEntity
{
    private SalesQuoteLine() { }

    public SalesQuoteLine(Guid tenantId, Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal taxRate) : base(tenantId)
    {
        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(sku) || sku.Length > 40) throw new DomainException("El producto de la cotización no es válido.");
        if (quantity is < 1 or > 100000) throw new DomainException("La cantidad debe estar entre 1 y 100,000.");
        if (unitPrice is < 0 or > 999999999999.99m || unitPrice != decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero))
            throw new DomainException("El precio unitario debe ser válido y tener hasta dos decimales.");
        if (taxRate is < 0 or > 1 || taxRate != decimal.Round(taxRate, 4, MidpointRounding.AwayFromZero)) throw new DomainException("La tasa de impuesto de la cotización no es válida.");
        ProductId = productId; Sku = sku.Trim(); ProductName = string.IsNullOrWhiteSpace(productName) ? throw new DomainException("El nombre de producto es obligatorio.") : productName.Trim();
        Quantity = quantity; UnitPrice = unitPrice; TaxRate = taxRate; LineTotal = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
    }

    public Guid SalesQuoteId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount => decimal.Round(LineTotal * TaxRate, 2, MidpointRounding.AwayFromZero);
}
