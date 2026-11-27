using OJunnySyS.Domain.Banking;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class BankingDomainTests
{
    [TestMethod]
    public void BankAccount_StoresOnlyFourDigitSuffixAndUsesOptimisticRevision()
    {
        var account = new BankAccount(Guid.NewGuid(), "Cuenta corriente", "Banco de prueba", "1234", "dop", Guid.NewGuid());

        Assert.AreEqual("1234", account.AccountSuffix);
        Assert.AreEqual("DOP", account.CurrencyCode);
        Assert.Throws<DomainException>(() => new BankAccount(Guid.NewGuid(), "Cuenta corriente", "Banco", "12345", "DOP", Guid.NewGuid()));
        account.SetActive(false, 0);
        Assert.AreEqual(1, account.Revision);
        Assert.Throws<ConcurrencyConflictException>(() => account.SetActive(true, 0));
    }

    [TestMethod]
    public void ReconciliationLine_CanOnlyMatchOnceAndOpenStatementCanBeCompleted()
    {
        var tenantId = Guid.NewGuid();
        var statement = new BankReconciliation(tenantId, Guid.NewGuid(), "EST-01", new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31), 100m, 150m);
        var line = statement.AddLine(new DateOnly(2026, 10, 4), "Depósito", 50m, "ABC-1");

        Assert.IsFalse(line.IsMatched);
        Assert.Throws<DomainException>(() => statement.MarkReconciled(DateTimeOffset.UtcNow));
        statement.MatchLine(line.Id, Guid.NewGuid());
        Assert.IsTrue(line.IsMatched);
        Assert.Throws<DomainException>(() => statement.MatchLine(line.Id, Guid.NewGuid()));
        statement.MarkReconciled(DateTimeOffset.UtcNow);
        Assert.AreEqual(BankReconciliationStatus.Reconciled, statement.Status);
        Assert.Throws<DomainException>(statement.EnsureOpen);
    }

    [TestMethod]
    public void StatementLine_RejectsZeroAmountAndInvalidDescription()
    {
        var tenantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var statement = new BankReconciliation(tenantId, Guid.NewGuid(), "EST-02", date, date, 0m, 0m);
        Assert.Throws<DomainException>(() => statement.AddLine(date, "Depósito", 0m));
        Assert.Throws<DomainException>(() => statement.AddLine(date, "", 1m));
    }
}
