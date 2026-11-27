using System.Globalization;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public enum BankCsvDateFormat { YearMonthDay, DayMonthYear, MonthDayYear }
public enum BankCsvNumberFormat { DecimalPoint, DecimalComma }

public sealed record BankCsvTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, char Delimiter);
public sealed record BankCsvMapping(int DateColumn, int DescriptionColumn, int AmountColumn, int? ReferenceColumn,
    BankCsvDateFormat DateFormat, BankCsvNumberFormat NumberFormat);
public sealed record BankCsvParseResult(IReadOnlyList<BankStatementLineRequest> Lines, IReadOnlyList<string> Errors);

/// <summary>Reads bank statement CSV files without persisting the original upload.</summary>
public static class BankStatementCsvParser
{
    public const int MaximumRows = 5000;
    public const int MaximumCharacters = 2_000_000;

    public static BankCsvTable ReadTable(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length > MaximumCharacters) throw new DomainException("El archivo CSV no puede superar 2 MB de texto.");
        content = content.TrimStart('\uFEFF');
        var delimiter = DetectDelimiter(content);
        var records = ParseRecords(content, delimiter).Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell))).ToList();
        if (records.Count < 2) throw new DomainException("El archivo necesita una fila de encabezados y al menos un movimiento.");
        var headers = records[0].Select(x => x.Trim()).ToArray();
        if (headers.Length < 3) throw new DomainException("El CSV debe tener al menos tres columnas para fecha, descripción e importe.");
        var rows = records.Skip(1).Select(x => (IReadOnlyList<string>)x).ToArray();
        if (rows.Length > MaximumRows) throw new DomainException($"El CSV supera el máximo de {MaximumRows:N0} movimientos.");
        return new BankCsvTable(headers, rows, delimiter);
    }

    public static BankCsvParseResult MapLines(BankCsvTable table, BankCsvMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(mapping);
        EnsureColumn(mapping.DateColumn, table.Headers.Count, "fecha");
        EnsureColumn(mapping.DescriptionColumn, table.Headers.Count, "descripción");
        EnsureColumn(mapping.AmountColumn, table.Headers.Count, "importe");
        if (mapping.DateColumn == mapping.DescriptionColumn || mapping.DateColumn == mapping.AmountColumn || mapping.DescriptionColumn == mapping.AmountColumn)
            throw new DomainException("Asigna una columna distinta para fecha, descripción e importe.");
        if (mapping.ReferenceColumn is { } referenceColumn)
        {
            EnsureColumn(referenceColumn, table.Headers.Count, "referencia");
            if (new[] { mapping.DateColumn, mapping.DescriptionColumn, mapping.AmountColumn }.Contains(referenceColumn))
                throw new DomainException("La referencia debe usar una columna diferente a fecha, descripción e importe.");
        }

        var lines = new List<BankStatementLineRequest>(table.Rows.Count);
        var errors = new List<string>();
        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var recordNumber = i + 2;
            if (row.Count < table.Headers.Count)
            {
                errors.Add($"Fila {recordNumber}: faltan columnas.");
                continue;
            }
            var dateText = row[mapping.DateColumn].Trim();
            var description = row[mapping.DescriptionColumn].Trim();
            var amountText = row[mapping.AmountColumn].Trim();
            if (!TryParseDate(dateText, mapping.DateFormat, out var date))
            {
                errors.Add($"Fila {recordNumber}: la fecha '{dateText}' no coincide con el formato seleccionado.");
                continue;
            }
            if (description.Length is < 2 or > 180)
            {
                errors.Add($"Fila {recordNumber}: la descripción debe tener entre 2 y 180 caracteres.");
                continue;
            }
            if (!TryParseAmount(amountText, mapping.NumberFormat, out var amount) || amount == 0 || amount != decimal.Round(amount, 2))
            {
                errors.Add($"Fila {recordNumber}: el importe '{amountText}' no es válido o tiene más de dos decimales.");
                continue;
            }
            var reference = mapping.ReferenceColumn is { } index ? row[index].Trim() : null;
            if (reference?.Length > 120)
            {
                errors.Add($"Fila {recordNumber}: la referencia no puede superar 120 caracteres.");
                continue;
            }
            lines.Add(new BankStatementLineRequest(date, description, amount, reference));
        }
        return new BankCsvParseResult(lines, errors);
    }

    private static char DetectDelimiter(string content)
    {
        var first = content.AsSpan();
        var quote = false;
        var candidates = new Dictionary<char, int> { [','] = 0, [';'] = 0, ['\t'] = 0, ['|'] = 0 };
        for (var i = 0; i < first.Length && first[i] is not '\r' and not '\n'; i++)
        {
            if (first[i] == '"')
            {
                if (quote && i + 1 < first.Length && first[i + 1] == '"') { i++; continue; }
                quote = !quote;
            }
            else if (!quote && candidates.ContainsKey(first[i])) candidates[first[i]]++;
        }
        return candidates.OrderByDescending(x => x.Value).First().Value > 0
            ? candidates.OrderByDescending(x => x.Value).First().Key
            : throw new DomainException("No se pudo identificar separador de columnas en el CSV.");
    }

    private static List<string[]> ParseRecords(string content, char delimiter)
    {
        var records = new List<string[]>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
                continue;
            }
            if (ch == '"' && field.Length == 0) { quoted = true; continue; }
            if (ch == delimiter) { row.Add(field.ToString()); field.Clear(); continue; }
            if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear();
                records.Add(row.ToArray()); row.Clear();
                continue;
            }
            field.Append(ch);
        }
        if (quoted) throw new DomainException("El CSV contiene un campo entre comillas sin cerrar.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row.ToArray()); }
        return records;
    }

    private static bool TryParseDate(string text, BankCsvDateFormat format, out DateOnly date)
    {
        var pattern = format switch
        {
            BankCsvDateFormat.YearMonthDay => "yyyy-MM-dd",
            BankCsvDateFormat.DayMonthYear => "dd/MM/yyyy",
            _ => "MM/dd/yyyy"
        };
        if (DateTime.TryParseExact(text, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = DateOnly.FromDateTime(parsed);
            return true;
        }
        date = default;
        return false;
    }

    private static bool TryParseAmount(string text, BankCsvNumberFormat format, out decimal amount)
    {
        text = text.Replace("\u00a0", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (format == BankCsvNumberFormat.DecimalComma)
            text = text.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
        else
            text = text.Replace(",", "", StringComparison.Ordinal);
        return decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount);
    }

    private static void EnsureColumn(int index, int count, string label)
    {
        if (index < 0 || index >= count) throw new DomainException($"Selecciona una columna válida para {label}.");
    }
}
