using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Tests;

[TestClass]
public sealed class BankStatementCsvParserTests
{
    [TestMethod]
    public void ReadTable_HandlesSemicolonDelimitedQuotedFieldsAndEscapedQuotes()
    {
        var table = BankStatementCsvParser.ReadTable("Fecha;Descripción;Importe;Ref\r\n27/11/2026;\"Pago; proveedor\";-1.234,56;\"AB\"\"1\"");

        Assert.AreEqual(';', table.Delimiter);
        Assert.AreEqual(4, table.Headers.Count);
        Assert.AreEqual("Pago; proveedor", table.Rows[0][1]);
        Assert.AreEqual("AB\"1", table.Rows[0][3]);
    }

    [TestMethod]
    public void MapLines_UsesExplicitDateAndNumberFormatsAndReportsInvalidRows()
    {
        var table = BankStatementCsvParser.ReadTable("Date,Description,Amount,Reference\n2026-11-27,Deposit,1250.50,A-1\nbad,Fee,-10.00,B-2");
        var result = BankStatementCsvParser.MapLines(table, new BankCsvMapping(0, 1, 2, 3,
            BankCsvDateFormat.YearMonthDay, BankCsvNumberFormat.DecimalPoint));

        Assert.AreEqual(1, result.Lines.Count);
        Assert.AreEqual(new DateOnly(2026, 11, 27), result.Lines[0].TransactionDate);
        Assert.AreEqual(1250.50m, result.Lines[0].Amount);
        Assert.AreEqual("A-1", result.Lines[0].Reference);
        Assert.AreEqual(1, result.Errors.Count);
        StringAssert.Contains(result.Errors[0], "Fila 3");
    }

    [TestMethod]
    public void ReadTable_RejectsUnclosedQuotedFieldsAndMissingRows()
    {
        Assert.Throws<DomainException>(() => BankStatementCsvParser.ReadTable("Fecha;Descripción;Importe\n01/10/2026;\"sin cerrar;10"));
        Assert.Throws<DomainException>(() => BankStatementCsvParser.ReadTable("Fecha;Descripción;Importe"));
    }
}
