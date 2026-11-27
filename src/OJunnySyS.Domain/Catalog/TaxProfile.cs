using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Catalog;

/// <summary>A reusable tenant-owned tax rate. Historical documents store the applied rate as a snapshot.</summary>
public sealed class TaxProfile : TenantEntity
{
    private TaxProfile() { }

    public TaxProfile(Guid tenantId, string code, string name, decimal rate) : base(tenantId)
    {
        Code = NormalizeCode(code);
        UpdateDetails(name, rate);
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Update(string name, decimal rate, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        UpdateDetails(name, rate);
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    public void SetActive(bool active, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsActive == active) return;
        IsActive = active;
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    private void UpdateDetails(string? name, decimal rate)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length is < 2 or > 80) throw new DomainException("El nombre del impuesto debe tener entre 2 y 80 caracteres.");
        if (rate is < 0 or > 1 || rate != decimal.Round(rate, 4, MidpointRounding.AwayFromZero))
            throw new DomainException("La tasa del impuesto debe estar entre 0 y 100 % con hasta cuatro decimales.");
        Name = value;
        Rate = rate;
    }

    private static string NormalizeCode(string? code)
    {
        var value = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (value.Length is < 2 or > 24 || !Regex.IsMatch(value, "^[A-Z0-9][A-Z0-9_-]*$"))
            throw new DomainException("El código del impuesto debe usar de 2 a 24 letras, números, guiones o guiones bajos.");
        return value;
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("El impuesto cambió en otra sesión. Recarga antes de guardar.");
    }
}
