using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Accounting;

public enum AccountType { Asset, Liability, Equity, Revenue, Expense }

public sealed class ChartAccount : TenantEntity
{
    private ChartAccount() { }

    public ChartAccount(Guid tenantId, string code, string name, AccountType type, bool isSystem = false, string? description = null) : base(tenantId)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        if (!Enum.IsDefined(type)) throw new DomainException("El tipo de cuenta no es válido.");
        Type = type;
        IsSystem = isSystem;
        Description = NormalizeDescription(description);
        IsActive = true;
    }

    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public AccountType Type { get; private set; }
    public bool CanReceivePayments => Type == AccountType.Asset && Code.StartsWith("10", StringComparison.Ordinal);
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Update(string code, string name, AccountType type, string? description, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsSystem) throw new DomainException("Las cuentas base del sistema no se pueden modificar.");
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        if (!Enum.IsDefined(type)) throw new DomainException("El tipo de cuenta no es válido.");
        Type = type;
        Description = NormalizeDescription(description);
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    public void SetActive(bool active, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsSystem && !active) throw new DomainException("Las cuentas base del sistema no se pueden desactivar.");
        if (IsActive == active) return;
        IsActive = active;
        Revision = checked(Revision + 1);
        MarkUpdated();
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("La cuenta cambió en otra sesión. Recarga antes de guardar.");
    }

    private static string NormalizeCode(string? code)
    {
        var value = code?.Trim() ?? "";
        if (value.Length is < 2 or > 20 || !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9.-]*$"))
            throw new DomainException("El código debe tener entre 2 y 20 letras, números, puntos o guiones.");
        return value.ToUpperInvariant();
    }

    private static string NormalizeName(string? name)
    {
        var value = name?.Trim() ?? "";
        if (value.Length is < 2 or > 120) throw new DomainException("El nombre de cuenta debe tener entre 2 y 120 caracteres.");
        return value;
    }

    private static string? NormalizeDescription(string? description)
    {
        var value = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (value?.Length > 240) throw new DomainException("La descripción no puede superar 240 caracteres.");
        return value;
    }
}
