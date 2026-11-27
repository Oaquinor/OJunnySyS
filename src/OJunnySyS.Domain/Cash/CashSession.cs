using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Cash;

public enum CashSessionStatus { Open, Closed }
public enum CashMovementKind { CashIn, CashOut }

/// <summary>A single named drawer shift within one company. Only a tenant-scoped transaction may open or close it.</summary>
public sealed class CashSession : TenantEntity
{
    private CashSession() { }

    public CashSession(Guid tenantId, string registerCode, decimal openingFloat, Guid openedByUserId) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(registerCode) || registerCode.Trim().Length > 40) throw new DomainException("El código de caja debe tener entre 1 y 40 caracteres.");
        if (openingFloat is < 0 or > 999999999999.99m) throw new DomainException("El fondo inicial está fuera del rango permitido.");
        if (openedByUserId == Guid.Empty) throw new DomainException("El usuario que abre la caja no es válido.");
        RegisterCode = registerCode.Trim().ToUpperInvariant();
        OpeningFloat = decimal.Round(openingFloat, 2);
        OpenedByUserId = openedByUserId;
        Status = CashSessionStatus.Open;
    }

    public string RegisterCode { get; private set; } = "";
    public decimal OpeningFloat { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public DateTimeOffset OpenedAtUtc => CreatedAtUtc;
    public CashSessionStatus Status { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public decimal? ExpectedCash { get; private set; }
    public decimal? CountedCash { get; private set; }
    public decimal? Difference => ExpectedCash is { } expected && CountedCash is { } counted ? decimal.Round(counted - expected, 2) : null;

    public void Close(Guid closedByUserId, decimal expectedCash, decimal countedCash)
    {
        if (Status != CashSessionStatus.Open) throw new DomainException("La caja ya está cerrada.");
        if (closedByUserId == Guid.Empty) throw new DomainException("El usuario que cierra la caja no es válido.");
        if (expectedCash is < 0 or > 999999999999.99m || countedCash is < 0 or > 999999999999.99m) throw new DomainException("El arqueo está fuera del rango permitido.");
        ClosedByUserId = closedByUserId;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        ExpectedCash = decimal.Round(expectedCash, 2);
        CountedCash = decimal.Round(countedCash, 2);
        Status = CashSessionStatus.Closed;
        MarkUpdated();
    }
}

/// <summary>Immutable cash drawer inflow or outflow recorded against an open shift.</summary>
public sealed class CashMovement : TenantEntity
{
    private CashMovement() { }

    public CashMovement(Guid tenantId, Guid sessionId, Guid userId, CashMovementKind kind, decimal amount, string description,
        Guid? idempotencyKey = null, Guid? cashPaymentMethodId = null, Guid? counterAccountId = null) : base(tenantId)
    {
        if (sessionId == Guid.Empty || userId == Guid.Empty) throw new DomainException("La sesión y el usuario del movimiento son obligatorios.");
        if (!Enum.IsDefined(kind)) throw new DomainException("El tipo de movimiento de caja no es válido.");
        if (amount is <= 0 or > 999999999999.99m || amount != decimal.Round(amount, 2, MidpointRounding.AwayFromZero))
            throw new DomainException("El importe del movimiento debe ser positivo, tener hasta dos decimales y estar dentro del límite permitido.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 200) throw new DomainException("La descripción del movimiento debe tener entre 1 y 200 caracteres.");
        if (idempotencyKey == Guid.Empty || cashPaymentMethodId == Guid.Empty || counterAccountId == Guid.Empty)
            throw new DomainException("La clave y las cuentas del movimiento de caja no son válidas.");
        if ((idempotencyKey is null) != (cashPaymentMethodId is null) || (idempotencyKey is null) != (counterAccountId is null))
            throw new DomainException("Los movimientos contables de caja requieren clave y cuentas asociadas.");
        SessionId = sessionId; UserId = userId; Kind = kind; Amount = decimal.Round(amount, 2); Description = description.Trim();
        IdempotencyKey = idempotencyKey; CashPaymentMethodId = cashPaymentMethodId; CounterAccountId = counterAccountId;
    }

    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public CashMovementKind Kind { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = "";
    public Guid? IdempotencyKey { get; private set; }
    public Guid? CashPaymentMethodId { get; private set; }
    public Guid? CounterAccountId { get; private set; }
}
