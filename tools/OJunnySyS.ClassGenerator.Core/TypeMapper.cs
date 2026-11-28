namespace OJunnySyS.ClassGenerator.Core;

public sealed record TypeMapping(string CsType, bool IsString = false);
public static class TypeMapper
{
    public static TypeMapping? Map(ColumnInfo column)
    {
        var type = column.DataType;
        var unsigned = column.ColumnType.Contains("unsigned", StringComparison.OrdinalIgnoreCase);
        if (type is "decimal" or "numeric" && column.Precision is > 28) return null;
        TypeMapping? mapped = type switch
        {
            "char" when column.ColumnType.StartsWith("char(36)", StringComparison.OrdinalIgnoreCase) && column.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase) => new("Guid"),
            "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" or "json" => new("string", true),
            "binary" or "varbinary" or "tinyblob" or "blob" or "mediumblob" or "longblob" => new("byte[]"),
            "tinyint" when column.ColumnType.StartsWith("tinyint(1)", StringComparison.OrdinalIgnoreCase) => new("bool"),
            "tinyint" => new(unsigned ? "byte" : "sbyte"), "smallint" => new(unsigned ? "ushort" : "short"), "mediumint" or "int" or "integer" => new(unsigned ? "uint" : "int"), "bigint" => new(unsigned ? "ulong" : "long"),
            "decimal" or "numeric" => new("decimal"), "float" => new("float"), "double" or "real" => new("double"),
            "date" => new("DateOnly"), "datetime" or "timestamp" => new("DateTime"), "time" => new("TimeSpan"), "year" => new("short"),
            "bit" when column.ColumnType.Equals("bit(1)", StringComparison.OrdinalIgnoreCase) => new("bool"),
            "bit" => new("ulong"),
            _ => null
        };
        if (mapped is null) return null;
        return column.Nullable ? mapped with { CsType = mapped.CsType + "?" } : mapped;
    }
}
