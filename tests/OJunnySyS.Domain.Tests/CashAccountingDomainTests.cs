using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class CashAccountingDomainTests
{
    [TestMethod]
    public void ManualCashMovement_RequiresCompleteIdempotencyAndLedgerReferences()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Assert.ThrowsExactly<DomainException>(() => new CashMovement(tenantId, sessionId, userId,
            CashMovementKind.CashIn, 25m, "Fondo para cambio", Guid.NewGuid()));

        var movement = new CashMovement(tenantId, sessionId, userId, CashMovementKind.CashIn,
            25m, " Fondo para cambio ", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.AreEqual(25m, movement.Amount);
        Assert.AreEqual("Fondo para cambio", movement.Description);
        Assert.IsNotNull(movement.IdempotencyKey);
        Assert.IsNotNull(movement.CashPaymentMethodId);
        Assert.IsNotNull(movement.CounterAccountId);
    }

    [TestMethod]
    public void ManualCashMovement_RejectsFractionalCents()
    {
        Assert.ThrowsExactly<DomainException>(() => new CashMovement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CashMovementKind.CashOut, 12.345m, "Compra menor"));
    }
}

