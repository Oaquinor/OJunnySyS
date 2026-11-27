using System.Globalization;
using System.Text;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Web.Presentation;

public static class SalesReportCsvWriter
{
    public static byte[] Write(SalesReport report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Tipo,Fecha,Concepto,Cantidad,Transacciones,Importe");
        Row(csv, "Resumen", $"{report.From:yyyy-MM-dd} a {report.To:yyyy-MM-dd}", "Ventas", "", report.SalesCount, report.Total);
        Row(csv, "Resumen", "", "Subtotal", "", report.SalesCount, report.Subtotal);
        Row(csv, "Resumen", "", "Impuestos", "", report.SalesCount, report.TaxAmount);
        foreach (var item in report.DailySales) Row(csv, "Ventas por día", item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Total", "", "", item.Total);
        foreach (var item in report.Payments) Row(csv, "Métodos de pago", "", item.Method, "", item.Sales, item.Total);
        foreach (var item in report.TopProducts) Row(csv, "Productos destacados", "", item.ProductName, item.Quantity, "", item.Revenue);
        foreach (var item in report.Sellers ?? []) Row(csv, "Vendedores", "", item.SellerName, "", item.Sales, item.Revenue);

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        return [.. Encoding.UTF8.GetPreamble(), .. content];
    }

    private static void Row(StringBuilder csv, string type, string date, string concept, object quantity, object transactions, decimal amount) =>
        csv.Append(Csv(type)).Append(',').Append(Csv(date)).Append(',').Append(Csv(concept)).Append(',')
            .Append(Csv(Convert.ToString(quantity, CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(Convert.ToString(transactions, CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(amount.ToString("0.00", CultureInfo.InvariantCulture))).AppendLine();

    private static string Csv(string value)
    {
        var safe = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
