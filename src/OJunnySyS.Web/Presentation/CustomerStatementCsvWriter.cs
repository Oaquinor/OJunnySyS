using System.Globalization;
using System.Text;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Abstractions;

namespace OJunnySyS.Web.Presentation;

public static class CustomerStatementCsvWriter
{
    public static byte[] Write(CustomerStatementReport report, IBusinessClock clock)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Fecha,Referencia,Tipo,Detalle,Vencimiento,Débito,Crédito,Saldo");
        csv.AppendLine(string.Join(',', CsvText.Escape("Cliente"), CsvText.Escape(report.CustomerName)));
        csv.AppendLine(string.Join(',', CsvText.Escape("Período"), CsvText.Escape($"{report.From:yyyy-MM-dd} a {report.To:yyyy-MM-dd}")));
        csv.AppendLine(string.Join(',', CsvText.Escape("Saldo de apertura"), Amount(report.OpeningBalance)));
        foreach (var entry in report.Entries)
            csv.Append(CsvText.Escape(clock.ToBusinessTime(entry.OccurredAtUtc).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))).Append(',')
                .Append(CsvText.Escape(entry.Reference)).Append(',')
                .Append(CsvText.Escape(entry.Type switch { CustomerStatementEntryType.Sale => "Venta", CustomerStatementEntryType.CreditNote => "Nota de crédito", _ => "Pago" })).Append(',')
                .Append(CsvText.Escape(entry.Description)).Append(',')
                .Append(CsvText.Escape(entry.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(',')
                .Append(Amount(entry.Debit)).Append(',').Append(Amount(entry.Credit)).Append(',').Append(Amount(entry.Balance)).AppendLine();
        csv.AppendLine(string.Join(',', CsvText.Escape("Totales del período"), CsvText.Escape("Débitos"), Amount(report.Invoiced), CsvText.Escape("Créditos"), Amount(report.Collected)));
        csv.AppendLine(string.Join(',', CsvText.Escape("Saldo al corte"), Amount(report.ClosingBalance)));
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    private static string Amount(decimal amount) => CsvText.Escape(amount.ToString("0.00", CultureInfo.InvariantCulture));
}
