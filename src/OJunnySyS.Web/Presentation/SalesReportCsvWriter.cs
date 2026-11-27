using System.Globalization;
using System.Text;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Web.Presentation;

public static class SalesReportCsvWriter
{
    public static byte[] Write(SalesReport report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Tipo,Fecha,Concepto,Cantidad,Transacciones,Importe,Costo conocido,Margen bruto,Unidades sin costo");
        Row(csv, "Resumen", $"{report.From:yyyy-MM-dd} a {report.To:yyyy-MM-dd}", "Ventas", "", report.SalesCount, report.Total);
        Row(csv, "Resumen", "", "Subtotal", "", report.SalesCount, report.Subtotal);
        Row(csv, "Resumen", "", "Impuestos", "", report.SalesCount, report.TaxAmount);
        foreach (var item in report.DailySales) Row(csv, "Ventas por día", item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Total", "", "", item.Total);
        foreach (var item in report.Payments) Row(csv, "Métodos de pago", "", item.Method, "", item.Sales, item.Total);
        foreach (var item in report.TopProducts) Row(csv, "Productos destacados", "", item.ProductName, item.Quantity, "", item.Revenue, item.Cost, item.GrossProfit, item.UncostedUnits);
        foreach (var item in report.Sellers ?? []) Row(csv, "Vendedores", "", item.SellerName, "", item.Sales, item.Revenue);
        if (report.Documents is { Count: > 0 })
        {
            csv.AppendLine();
            csv.AppendLine("Número,Documento relacionado,Tipo,Fecha,Cliente,Vendedor,Forma de pago,Subtotal,Impuestos,Total,Cobrado,Saldo actual");
            foreach (var item in report.Documents)
                csv.Append(Csv(item.Number)).Append(',').Append(Csv(item.RelatedSaleNumber ?? "")).Append(',').Append(Csv(item.Type)).Append(',')
                    .Append(Csv(item.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))).Append(',').Append(Csv(item.CustomerName)).Append(',')
                    .Append(Csv(item.SellerName ?? "")).Append(',').Append(Csv(item.PaymentMethod)).Append(',')
                    .Append(item.Subtotal.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.TaxAmount.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.Total.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.Collected.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.Balance.ToString("0.00", CultureInfo.InvariantCulture)).AppendLine();
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        return [.. Encoding.UTF8.GetPreamble(), .. content];
    }

    private static void Row(StringBuilder csv, string type, string date, string concept, object quantity, object transactions, decimal amount, decimal? cost = null, decimal? grossProfit = null, int? uncostedUnits = null) =>
        csv.Append(Csv(type)).Append(',').Append(Csv(date)).Append(',').Append(Csv(concept)).Append(',')
            .Append(Csv(Convert.ToString(quantity, CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(Convert.ToString(transactions, CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(amount.ToString("0.00", CultureInfo.InvariantCulture))).Append(',')
            .Append(Csv(cost?.ToString("0.00", CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(grossProfit?.ToString("0.00", CultureInfo.InvariantCulture) ?? "")).Append(',')
            .Append(Csv(uncostedUnits?.ToString(CultureInfo.InvariantCulture) ?? "")).AppendLine();

    private static string Csv(string value) => CsvText.Escape(value);
}
