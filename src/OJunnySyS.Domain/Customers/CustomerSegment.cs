using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Customers;

public sealed class CustomerSegment : TenantEntity
{
    private CustomerSegment() { }

    public CustomerSegment(Guid tenantId, string name, Guid? requiredTagId, decimal? minimumLifetimeValue,
        decimal? maximumLifetimeValue, int? minimumSalesCount, int? purchasedWithinDays, bool activeCustomersOnly) : base(tenantId)
    {
        Update(name, requiredTagId, minimumLifetimeValue, maximumLifetimeValue, minimumSalesCount, purchasedWithinDays, activeCustomersOnly);
    }

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public Guid? RequiredTagId { get; private set; }
    public decimal? MinimumLifetimeValue { get; private set; }
    public decimal? MaximumLifetimeValue { get; private set; }
    public int? MinimumSalesCount { get; private set; }
    public int? PurchasedWithinDays { get; private set; }
    public bool ActiveCustomersOnly { get; private set; } = true;
    public long Revision { get; private set; }

    public void Update(string name, Guid? requiredTagId, decimal? minimumLifetimeValue, decimal? maximumLifetimeValue,
        int? minimumSalesCount, int? purchasedWithinDays, bool activeCustomersOnly)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new DomainException("El nombre del segmento debe tener entre 1 y 100 caracteres.");
        if (minimumLifetimeValue < 0 || maximumLifetimeValue < 0)
            throw new DomainException("El valor de compra no puede ser negativo.");
        if (minimumLifetimeValue is { } minimum && maximumLifetimeValue is { } maximum && minimum > maximum)
            throw new DomainException("El valor mínimo no puede superar al valor máximo.");
        if (minimumSalesCount is < 1 or > 100000) throw new DomainException("La cantidad mínima de compras debe estar entre 1 y 100,000.");
        if (purchasedWithinDays is < 1 or > 3650) throw new DomainException("Los días de compra reciente deben estar entre 1 y 3,650.");

        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        RequiredTagId = requiredTagId;
        MinimumLifetimeValue = minimumLifetimeValue;
        MaximumLifetimeValue = maximumLifetimeValue;
        MinimumSalesCount = minimumSalesCount;
        PurchasedWithinDays = purchasedWithinDays;
        ActiveCustomersOnly = activeCustomersOnly;
        Revision++;
        MarkUpdated();
    }
}
