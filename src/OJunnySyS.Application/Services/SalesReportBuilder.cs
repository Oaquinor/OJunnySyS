using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Services;

/// <summary>Builds net sales metrics, deducting credit notes in the period they were issued.</summary>
public static class SalesReportBuilder
{
    public static SalesReport Build(DateOnly from, DateOnly to, DateTimeOffset startUtc, DateTimeOffset endUtc,
        IReadOnlyCollection<Sale> sales, IBusinessClock clock)
    {
        var completed = sales.Where(x => x.Status == SaleStatus.Completed).ToArray();
        var issued = completed.Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc).ToArray();
        var returns = completed.SelectMany(sale => sale.Returns.Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .Select(credit => (Sale: sale, Credit: credit))).ToArray();
        var refunds = returns.Where(x => x.Credit.RefundAmount > 0).ToArray();

        var paymentRows = issued.SelectMany(sale => sale.Payments.Select(payment =>
                (SaleId: sale.Id, Method: payment.MethodName, Amount: payment.Amount)))
            .Concat(refunds.Select(x => (SaleId: x.Sale.Id, Method: x.Credit.PaymentMethodName ?? "Reembolso", Amount: -x.Credit.RefundAmount)));
        var paymentBreakdown = paymentRows.GroupBy(x => x.Method)
            .Select(group => new PaymentBreakdown(group.Key, group.Select(x => x.SaleId).Distinct().Count(), group.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Total).ToArray();

        var productRows = issued.SelectMany(sale => sale.Lines.Select(line => (Name: line.ProductName, Quantity: line.Quantity,
                Revenue: line.LineTotal, Cost: line.CostTotal, Uncosted: line.UncostedQuantity)))
            .Concat(returns.SelectMany(x => x.Credit.Lines.Select(line => (Name: line.ProductName, Quantity: -line.Quantity,
                Revenue: -line.LineTotal, Cost: -line.CostTotal, Uncosted: -line.UncostedQuantity))))
            .GroupBy(x => x.Name)
            .Select(group =>
            {
                var revenue = group.Sum(x => x.Revenue);
                var cost = group.Sum(x => x.Cost);
                return new ProductSalesSummary(group.Key, group.Sum(x => x.Quantity), revenue, cost, revenue - cost, group.Sum(x => x.Uncosted));
            })
            .Where(x => x.Revenue != 0 || x.Quantity != 0)
            .OrderByDescending(x => x.Revenue).Take(5).ToArray();

        var dailyRows = issued.Select(sale => (Date: DateOnly.FromDateTime(clock.ToBusinessTime(sale.CreatedAtUtc).DateTime), Amount: sale.Total))
            .Concat(returns.Select(x => (Date: DateOnly.FromDateTime(clock.ToBusinessTime(x.Credit.CreatedAtUtc).DateTime), Amount: -x.Credit.Total)));
        var dailySales = dailyRows.GroupBy(x => x.Date)
            .Select(group => new DailySalesSummary(group.Key, group.Sum(x => x.Amount))).OrderBy(x => x.Date).ToArray();

        var reportSaleIds = issued.Select(x => x.Id).Concat(returns.Select(x => x.Sale.Id)).Distinct().ToArray();
        var salesById = completed.ToDictionary(x => x.Id);
        var reportSales = reportSaleIds.Select(id => salesById[id]).ToArray();
        var sellers = reportSales.GroupBy(x => x.SellerName ?? "Sin vendedor")
            .Select(group =>
            {
                var ids = group.Select(x => x.Id).ToHashSet();
                var count = issued.Count(x => ids.Contains(x.Id));
                var revenue = issued.Where(x => ids.Contains(x.Id)).Sum(x => x.Total) - returns.Where(x => ids.Contains(x.Sale.Id)).Sum(x => x.Credit.Total);
                return new SellerSalesSummary(group.Key, count, revenue);
            }).OrderByDescending(x => x.Revenue).ToArray();
        var customers = reportSales.GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(group =>
            {
                var ids = group.Select(x => x.Id).ToHashSet();
                var groupIssued = issued.Where(x => ids.Contains(x.Id)).ToArray();
                var groupReturns = returns.Where(x => ids.Contains(x.Sale.Id)).ToArray();
                return new CustomerSalesSummary(group.Key.CustomerId, group.Key.CustomerName, groupIssued.Length,
                    groupIssued.Sum(x => x.Subtotal) - groupReturns.Sum(x => x.Credit.Subtotal),
                    groupIssued.Sum(x => x.TaxAmount) - groupReturns.Sum(x => x.Credit.TaxAmount),
                    groupIssued.Sum(x => x.Total) - groupReturns.Sum(x => x.Credit.Total));
            }).OrderByDescending(x => x.Total).ToArray();
        var warehouses = reportSales.GroupBy(x => x.WarehouseId)
            .Select(group =>
            {
                var ids = group.Select(x => x.Id).ToHashSet();
                var groupIssued = issued.Where(x => ids.Contains(x.Id)).ToArray();
                var groupReturns = returns.Where(x => ids.Contains(x.Sale.Id)).ToArray();
                return new WarehouseSalesSummary(group.Key, groupIssued.Length,
                    groupIssued.Sum(x => x.Subtotal) - groupReturns.Sum(x => x.Credit.Subtotal),
                    groupIssued.Sum(x => x.TaxAmount) - groupReturns.Sum(x => x.Credit.TaxAmount),
                    groupIssued.Sum(x => x.Total) - groupReturns.Sum(x => x.Credit.Total));
            }).OrderByDescending(x => x.Total).ToArray();

        return new SalesReport(from, to, issued.Length,
            issued.Sum(x => x.Subtotal) - returns.Sum(x => x.Credit.Subtotal),
            issued.Sum(x => x.TaxAmount) - returns.Sum(x => x.Credit.TaxAmount),
            issued.Sum(x => x.Total) - returns.Sum(x => x.Credit.Total), paymentBreakdown, productRows, dailySales, sellers, customers, warehouses);
    }
}
