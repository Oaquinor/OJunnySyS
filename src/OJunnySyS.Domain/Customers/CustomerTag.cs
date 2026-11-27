using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Customers;

public enum CustomerTagColor { Green, Blue, Purple, Amber, Rose, Slate }

public sealed class CustomerTag : TenantEntity
{
    private CustomerTag() { }

    public CustomerTag(Guid tenantId, string name, CustomerTagColor color) : base(tenantId) => Update(name, color);

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public CustomerTagColor Color { get; private set; }

    public void Update(string name, CustomerTagColor color)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 40)
            throw new DomainException("La etiqueta debe tener entre 1 y 40 caracteres.");
        if (!Enum.IsDefined(color)) throw new DomainException("El color de la etiqueta no es válido.");
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Color = color;
        MarkUpdated();
    }
}
