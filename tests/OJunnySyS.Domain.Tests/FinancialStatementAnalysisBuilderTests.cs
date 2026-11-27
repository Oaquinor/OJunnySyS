using OJunnySyS.Application.Services;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class FinancialStatementAnalysisBuilderTests
{
    [TestMethod]
    public void VerticalPercent_UsesStatementBaseAndHandlesZero()
    {
        Assert.AreEqual(25m, FinancialStatementAnalysisBuilder.VerticalPercent(250m, 1000m));
        Assert.AreEqual(0m, FinancialStatementAnalysisBuilder.VerticalPercent(250m, 0m));
    }

    [TestMethod]
    public void ComparisonBarWidth_ScalesBothPositiveAndNegativeValuesToLargestMagnitude()
    {
        Assert.AreEqual(50m, FinancialStatementAnalysisBuilder.ComparisonBarWidth(50m, 100m));
        Assert.AreEqual(100m, FinancialStatementAnalysisBuilder.ComparisonBarWidth(-100m, 50m));
        Assert.AreEqual(0m, FinancialStatementAnalysisBuilder.ComparisonBarWidth(0m, 0m));
    }

    [TestMethod]
    public void PercentageChange_UsesAbsolutePriorBalanceAndLabelsNewAmounts()
    {
        Assert.AreEqual(.25m, FinancialStatementAnalysisBuilder.PercentageChange(125m, 100m));
        Assert.AreEqual(2m, FinancialStatementAnalysisBuilder.PercentageChange(100m, -100m));
        Assert.IsNull(FinancialStatementAnalysisBuilder.PercentageChange(10m, 0m));
        Assert.AreEqual("Nuevo", FinancialStatementAnalysisBuilder.FormatPercentageChange(10m, 0m));
        Assert.AreEqual("0 %", FinancialStatementAnalysisBuilder.FormatPercentageChange(0m, 0m));
        var formatted = FinancialStatementAnalysisBuilder.FormatPercentageChange(125m, 100m);
        StringAssert.Contains(formatted, "25");
        StringAssert.EndsWith(formatted, "%");
    }
}
