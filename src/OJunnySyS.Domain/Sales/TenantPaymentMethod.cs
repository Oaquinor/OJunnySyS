using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Sales;

public enum PaymentMethodType { Cash, Card, Transfer, Other }

public sealed class TenantPaymentMethod : TenantEntity
{
    private TenantPaymentMethod() { }

    public TenantPaymentMethod(Guid tenantId, string code, string name, PaymentMethodType type,
        string? instructions = null, string? accountLabel = null, string? accountReference = null, string? qrPayload = null) : base(tenantId)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        if (!Enum.IsDefined(type)) throw new DomainException("El tipo de medio de pago no es válido.");
        Type = type;
        UpdateDetails(instructions, accountLabel, accountReference, qrPayload);
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PaymentMethodType Type { get; private set; }
    public string? Instructions { get; private set; }
    public string? AccountLabel { get; private set; }
    public string? AccountReference { get; private set; }
    public string? QrPayload { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Update(string name, string? instructions, string? accountLabel, string? accountReference, string? qrPayload, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        Name = NormalizeName(name);
        UpdateDetails(instructions, accountLabel, accountReference, qrPayload);
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

    private void UpdateDetails(string? instructions, string? accountLabel, string? accountReference, string? qrPayload)
    {
        Instructions = NormalizeOptional(instructions, 300, "Las instrucciones");
        AccountLabel = NormalizeOptional(accountLabel, 120, "El nombre de cuenta");
        AccountReference = NormalizeOptional(accountReference, 100, "La referencia de cuenta");
        QrPayload = NormalizeOptional(qrPayload, 2000, "El contenido QR");
    }

    private static string NormalizeName(string? name)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length is < 2 or > 80) throw new DomainException("El nombre del medio de pago debe tener entre 2 y 80 caracteres.");
        return value;
    }

    private static string NormalizeCode(string? code)
    {
        var value = code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Length is < 2 or > 40 || !Regex.IsMatch(value, "^[a-z0-9][a-z0-9_-]*$"))
            throw new DomainException("El código interno del medio de pago no es válido.");
        return value;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string field)
    {
        value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (value?.Length > maxLength) throw new DomainException($"{field} no puede superar {maxLength} caracteres.");
        return value;
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("El medio de pago cambió en otra sesión. Recarga antes de guardar.");
    }
}

public sealed class SalePayment : TenantEntity
{
    private SalePayment() { }

    public SalePayment(Guid tenantId, Guid saleId, Guid paymentMethodId, string methodName, PaymentMethodType type,
        decimal amount, string? reference) : base(tenantId)
    {
        if (saleId == Guid.Empty || paymentMethodId == Guid.Empty) throw new DomainException("La forma de pago no está vinculada correctamente a la venta.");
        if (string.IsNullOrWhiteSpace(methodName) || methodName.Trim().Length > 80) throw new DomainException("El nombre del medio de pago no es válido.");
        if (!Enum.IsDefined(type)) throw new DomainException("El tipo de medio de pago no es válido.");
        if (amount <= 0 || amount != decimal.Round(amount, 2)) throw new DomainException("Cada importe de pago debe ser positivo y tener hasta dos decimales.");
        if (reference?.Trim().Length > 120) throw new DomainException("La referencia del pago no puede superar 120 caracteres.");
        SaleId = saleId; PaymentMethodId = paymentMethodId; MethodName = methodName.Trim(); Type = type;
        Amount = amount; Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    public Guid SaleId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public string MethodName { get; private set; } = string.Empty;
    public PaymentMethodType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
}

public sealed record PaymentAllocation(Guid PaymentMethodId, string MethodName, PaymentMethodType Type, decimal Amount, string? Reference = null);
