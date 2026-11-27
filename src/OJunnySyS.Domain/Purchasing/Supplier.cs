using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Purchasing;

public sealed class Supplier : TenantEntity
{
    private Supplier() { }

    public Supplier(Guid tenantId, string name, string? taxIdentifier, string email, string phone, string? notes = null) : base(tenantId)
    {
        UpdateContact(name, taxIdentifier, email, phone, notes);
        IsActive = true;
    }

    public string Name { get; private set; } = "";
    public string? TaxIdentifier { get; private set; }
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void UpdateContact(string name, string? taxIdentifier, string email, string phone, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre del proveedor es obligatorio.");
        email ??= string.Empty;
        phone ??= string.Empty;
        if (name.Trim().Length > 160 || (taxIdentifier?.Trim().Length ?? 0) > 40 || email.Trim().Length > 254 || phone.Trim().Length > 40 || (notes?.Trim().Length ?? 0) > 1000)
            throw new DomainException("Uno de los datos del proveedor supera el largo permitido.");
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@', StringComparison.Ordinal)) throw new DomainException("El correo del proveedor no es válido.");
        Name = name.Trim();
        TaxIdentifier = string.IsNullOrWhiteSpace(taxIdentifier) ? null : taxIdentifier.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Revision++;
        MarkUpdated();
    }

    public void Deactivate() { if (!IsActive) return; IsActive = false; Revision++; MarkUpdated(); }
}
