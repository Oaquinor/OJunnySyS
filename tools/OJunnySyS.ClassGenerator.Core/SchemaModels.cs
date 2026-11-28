namespace OJunnySyS.ClassGenerator.Core;

public sealed record TableInfo(string Name, string Kind);
public sealed record ColumnInfo(string Name, string DataType, string ColumnType, bool Nullable, bool AutoIncrement, int? Length, int? Precision, int? Scale, string? DefaultValue, int Ordinal);
public sealed record KeyInfo(string Name, IReadOnlyList<string> Columns, bool Unique, string Kind);
public sealed record ForeignKeyInfo(string Name, IReadOnlyList<string> Columns, string ReferencedSchema, string ReferencedTable, IReadOnlyList<string> ReferencedColumns);
public sealed record TableSchema(string Schema, string Table, string Kind, IReadOnlyList<ColumnInfo> Columns, IReadOnlyList<KeyInfo> Keys)
{
    public IReadOnlyList<string> PrimaryKey => Keys.FirstOrDefault(x => x.Kind == "PRIMARY")?.Columns ?? [];
    public IReadOnlyList<ForeignKeyInfo> ForeignKeys { get; init; } = [];
}
