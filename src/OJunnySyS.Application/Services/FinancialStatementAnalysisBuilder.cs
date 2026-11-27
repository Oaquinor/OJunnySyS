namespace OJunnySyS.Application.Services;

/// <summary>Pure calculations shared by financial statement views and exports.</summary>
public static class FinancialStatementAnalysisBuilder
{
    public static decimal VerticalPercent(decimal amount, decimal baseAmount) =>
        baseAmount == 0 ? 0 : amount / baseAmount * 100;

    public static decimal ComparisonBarWidth(decimal value, decimal other)
    {
        var largest = Math.Max(Math.Abs(value), Math.Abs(other));
        return largest == 0 ? 0 : Math.Round(Math.Abs(value) / largest * 100, 1);
    }

    public static decimal? PercentageChange(decimal current, decimal previous) =>
        previous == 0 ? null : (current - previous) / Math.Abs(previous);

    public static string FormatPercentageChange(decimal current, decimal previous) =>
        PercentageChange(current, previous) is { } change
            ? $"{change:+0.0%;-0.0%;0.0%}"
            : current == 0 ? "0 %" : "Nuevo";
}
