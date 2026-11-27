namespace OJunnySyS.Web.Presentation;

public static class CsvText
{
    public static string Escape(string? value)
    {
        value ??= string.Empty;
        var safe = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
