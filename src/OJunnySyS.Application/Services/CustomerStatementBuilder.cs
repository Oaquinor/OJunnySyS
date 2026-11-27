using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

/// <summary>Builds a customer ledger statement from persisted sale and collection snapshots.</summary>
public static class CustomerStatementBuilder
{
    public static CustomerStatementReport Build(Guid customerId, string customerName, DateOnly from, DateOnly to,
        CustomerStatementData data)
    {
        var period = data.Movements.OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Type == CustomerStatementEntryType.Sale ? 0 : 1)
            .ThenBy(x => x.Reference, StringComparer.Ordinal).ToArray();
        var openingBalance = decimal.Round(data.OpeningBalance, 2, MidpointRounding.AwayFromZero);
        var balance = openingBalance;
        var entries = period.Select(transaction =>
        {
            balance = decimal.Round(balance + transaction.Debit - transaction.Credit, 2, MidpointRounding.AwayFromZero);
            return new CustomerStatementLine(transaction.SaleId, transaction.OccurredAtUtc, transaction.Reference, transaction.Type,
                transaction.Description, transaction.DueDate, transaction.Debit, transaction.Credit, balance);
        }).ToArray();
        return new CustomerStatementReport(customerId, customerName, from, to, openingBalance,
            period.Sum(x => x.Debit), period.Sum(x => x.Credit), balance, entries);
    }
}
