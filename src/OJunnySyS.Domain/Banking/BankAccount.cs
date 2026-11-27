using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Banking;

/// <summary>A tenant-owned bank account linked to exactly one asset account in the general ledger.</summary>
public sealed class BankAccount : TenantEntity
{
    private BankAccount() { }

    public BankAccount(Guid tenantId, string name, string institution, string? accountSuffix, string currencyCode,
        Guid ledgerAccountId) : base(tenantId)
    {
        Name = NormalizeRequired(name, 2, 80, "El nombre de la cuenta");
        Institution = NormalizeRequired(institution, 2, 100, "La institución bancaria");
        AccountSuffix = NormalizeSuffix(accountSuffix);
        CurrencyCode = NormalizeCurrency(currencyCode);
        if (ledgerAccountId == Guid.Empty) throw new DomainException("La cuenta contable asociada es obligatoria.");
        LedgerAccountId = ledgerAccountId;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public string Institution { get; private set; } = string.Empty;
    /// <summary>Only the last four digits are stored; online banking credentials and full account numbers are never stored.</summary>
    public string? AccountSuffix { get; private set; }
    public string CurrencyCode { get; private set; } = "DOP";
    public Guid LedgerAccountId { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Update(string name, string institution, string? accountSuffix, string currencyCode, Guid ledgerAccountId,
        long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        Name = NormalizeRequired(name, 2, 80, "El nombre de la cuenta");
        Institution = NormalizeRequired(institution, 2, 100, "La institución bancaria");
        AccountSuffix = NormalizeSuffix(accountSuffix);
        CurrencyCode = NormalizeCurrency(currencyCode);
        if (ledgerAccountId == Guid.Empty) throw new DomainException("La cuenta contable asociada es obligatoria.");
        LedgerAccountId = ledgerAccountId;
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

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("La cuenta bancaria cambió en otra sesión. Recarga antes de guardar.");
    }

    private static string NormalizeRequired(string? value, int min, int max, string label)
    {
        value = value?.Trim() ?? string.Empty;
        if (value.Length < min || value.Length > max) throw new DomainException($"{label} debe tener entre {min} y {max} caracteres.");
        return value;
    }

    private static string? NormalizeSuffix(string? suffix)
    {
        suffix = string.IsNullOrWhiteSpace(suffix) ? null : suffix.Trim();
        if (suffix is not null && !Regex.IsMatch(suffix, "^[0-9]{4}$"))
            throw new DomainException("Guarda únicamente los últimos cuatro dígitos de la cuenta bancaria.");
        return suffix;
    }

    private static string NormalizeCurrency(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Regex.IsMatch(normalized, "^[A-Z]{3}$")) throw new DomainException("El código de moneda debe usar tres letras ISO.");
        return normalized;
    }
}
