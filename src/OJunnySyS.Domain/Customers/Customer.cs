using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Customers;

public sealed class Customer : TenantEntity
{
    private Customer() { }

    public Customer(Guid tenantId, string firstName, string lastName, string email, string phone, string? company = null) : base(tenantId)
    {
        UpdateContact(firstName, lastName, email, phone, company);
        IsActive = true;
    }

    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string? Company { get; private set; }
    public bool IsActive { get; private set; }
    public decimal LifetimeValue { get; private set; }
    public long Revision { get; private set; }

    public void UpdateContact(string firstName, string lastName, string email, string phone, string? company)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal)) throw new DomainException("El correo del cliente no es válido.");
        if (firstName.Trim().Length > 100 || lastName.Trim().Length > 100 || email.Trim().Length > 254 || phone.Trim().Length > 40 || company?.Trim().Length > 160)
            throw new DomainException("Uno o más campos exceden el largo permitido.");
        FirstName = firstName.Trim(); LastName = lastName.Trim(); Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim(); Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim(); MarkChanged();
    }

    public void Deactivate() { if (!IsActive) return; IsActive = false; MarkChanged(); }
    public void RecordPurchase(decimal amount) { if (amount <= 0) throw new DomainException("El importe debe ser mayor que cero."); LifetimeValue += amount; MarkChanged(); }

    private void MarkChanged() { Revision++; MarkUpdated(); }
}
