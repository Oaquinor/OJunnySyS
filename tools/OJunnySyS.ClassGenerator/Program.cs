using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

return await ClassGeneratorCli.RunAsync(args);

internal static class ClassGeneratorCli
{
    private const string Usage = "Uso:\n  dotnet run --project tools/OJunnySyS.ClassGenerator -- tables <esquema>\n  dotnet run --project tools/OJunnySyS.ClassGenerator -- preview <esquema> <tabla> [--class Nombre]\n  dotnet run --project tools/OJunnySyS.ClassGenerator -- generate <esquema> <tabla> --output <directorio> [--class Nombre]\nConfigura OJUNNYSYS_GENERATOR_CONNECTION con un usuario MySQL de solo lectura.";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "--help" or "-h") { Console.WriteLine(Usage); return 0; }
            var connectionString = Environment.GetEnvironmentVariable("OJUNNYSYS_GENERATOR_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Define OJUNNYSYS_GENERATOR_CONNECTION con una conexión MySQL de solo lectura.");
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            switch (args[0].ToLowerInvariant())
            {
                case "tables" when args.Length == 2:
                    foreach (var table in await SchemaReader.GetTablesAsync(connection, args[1])) Console.WriteLine($"{table.Kind,-8} {table.Name}");
                    return 0;
                case "preview" when args.Length >= 3:
                case "generate" when args.Length >= 3:
                    var options = Options.Parse(args);
                    var schema = await SchemaReader.ReadAsync(connection, options.Schema, options.Table);
                    var files = CodeGenerator.Create(schema, options.ClassName);
                    foreach (var file in files) Console.WriteLine($"--- {file.Path} ---\n{file.Content}");
                    if (options.Command == "generate")
                    {
                        var root = Path.GetFullPath(options.Output ?? throw new ArgumentException("Falta --output."));
                        var destinations = files.Select(file => (File: file, Path: Path.GetFullPath(Path.Combine(root, file.Path)))).ToArray();
                        if (destinations.Any(x => { var relative = Path.GetRelativePath(root, x.Path); return relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative); }))
                            throw new InvalidOperationException("La salida contiene una ruta fuera del directorio indicado.");
                        var existing = destinations.Where(x => File.Exists(x.Path)).Select(x => x.Path).ToArray();
                        if (existing.Length > 0) throw new IOException("No se sobrescribió ningún archivo. Ya existen:\n" + string.Join("\n", existing));
                        var created = new List<string>();
                        try
                        {
                            foreach (var item in destinations)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(item.Path)!);
                                await using var stream = new FileStream(item.Path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                                created.Add(item.Path);
                                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                                await writer.WriteAsync(item.File.Content);
                            }
                        }
                        catch
                        {
                            foreach (var path in created) try { File.Delete(path); } catch (IOException) { }
                            throw;
                        }
                        Console.WriteLine($"Generados {destinations.Length} archivos nuevos en {root}");
                    }
                    return 0;
                default: Console.Error.WriteLine(Usage); return 2;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }
}

internal sealed record Options(string Command, string Schema, string Table, string? ClassName, string? Output)
{
    public static Options Parse(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        if (args.Length < 3) throw new ArgumentException("Indica esquema y tabla.");
        string? className = null, output = null;
        for (var i = 3; i < args.Length; i++)
        {
            if (args[i] is "--class" or "--output" && i + 1 < args.Length)
            {
                var value = args[++i];
                if (args[i - 1] == "--class") className = value;
                else output = value;
            }
            else throw new ArgumentException($"Opción no reconocida: {args[i]}");
        }
        if (className is not null && (!Regex.IsMatch(className, "^[A-Za-z_][A-Za-z0-9_]*$") || CSharpKeywords.Contains(className))) throw new ArgumentException("--class requiere un identificador C# válido que no sea palabra reservada.");
        if (command == "generate" && output is null) throw new ArgumentException("generate requiere --output.");
        return new(command, args[1], args[2], className, output);
    }

    private static readonly HashSet<string> CSharpKeywords = ["abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"];
}

internal sealed record TableInfo(string Name, string Kind);
internal sealed record ColumnInfo(string Name, string DataType, string ColumnType, bool Nullable, bool AutoIncrement, int? Length, int? Precision, int? Scale, string? DefaultValue, int Ordinal);
internal sealed record KeyInfo(string Name, IReadOnlyList<string> Columns, bool Unique, string Kind);
internal sealed record ForeignKeyInfo(string Name, IReadOnlyList<string> Columns, string ReferencedSchema, string ReferencedTable, IReadOnlyList<string> ReferencedColumns);
internal sealed record TableSchema(string Schema, string Table, string Kind, IReadOnlyList<ColumnInfo> Columns, IReadOnlyList<KeyInfo> Keys)
{
    public IReadOnlyList<string> PrimaryKey => Keys.FirstOrDefault(x => x.Kind == "PRIMARY")?.Columns ?? [];
    public IReadOnlyList<ForeignKeyInfo> ForeignKeys { get; init; } = [];
}

internal static class SchemaReader
{
    public static async Task<IReadOnlyList<TableInfo>> GetTablesAsync(MySqlConnection connection, string schema)
    {
        const string sql = "SELECT TABLE_NAME, TABLE_TYPE FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<TableInfo>();
        while (await reader.ReadAsync()) result.Add(new(reader.GetString(0), reader.GetString(1)));
        return result;
    }

    public static async Task<TableSchema> ReadAsync(MySqlConnection connection, string schema, string table)
    {
        var tables = await GetTablesAsync(connection, schema);
        var tableInfo = tables.SingleOrDefault(x => x.Name == table) ?? throw new InvalidOperationException($"No existe {schema}.{table} o no tienes permiso para verla.");
        if (tableInfo.Kind != "BASE TABLE") throw new InvalidOperationException("Las vistas no se generan como entidades editables.");
        const string columnSql = "SELECT COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, IS_NULLABLE, EXTRA, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, COLUMN_DEFAULT, ORDINAL_POSITION FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        await using var columnCommand = new MySqlCommand(columnSql, connection);
        columnCommand.Parameters.AddWithValue("@schema", schema);
        columnCommand.Parameters.AddWithValue("@table", table);
        var columns = new List<ColumnInfo>();
        await using (var reader = await columnCommand.ExecuteReaderAsync())
            while (await reader.ReadAsync()) columns.Add(new(reader.GetString(0), reader.GetString(1).ToLowerInvariant(), reader.GetString(2), reader.GetString(3) == "YES", reader.GetString(4).Contains("auto_increment", StringComparison.OrdinalIgnoreCase), reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)), reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)), reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7)), reader.IsDBNull(8) ? null : Convert.ToString(reader.GetValue(8)), reader.GetInt32(9)));
        const string keySql = "SELECT INDEX_NAME, COLUMN_NAME, NON_UNIQUE, SEQ_IN_INDEX FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY INDEX_NAME, SEQ_IN_INDEX";
        await using var keyCommand = new MySqlCommand(keySql, connection);
        keyCommand.Parameters.AddWithValue("@schema", schema);
        keyCommand.Parameters.AddWithValue("@table", table);
        var keys = new List<KeyInfo>();
        await using (var reader = await keyCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var column = reader.GetString(1);
                var unique = reader.GetInt32(2) == 0;
                var kind = name == "PRIMARY" ? "PRIMARY" : "INDEX";
                var index = keys.FindIndex(x => x.Name == name);
                if (index < 0) keys.Add(new(name, [column], unique, kind));
                else keys[index] = keys[index] with { Columns = keys[index].Columns.Append(column).ToArray() };
            }
        }
        if (columns.Count == 0) throw new InvalidOperationException("La tabla no contiene columnas visibles.");
        if (keys.All(x => x.Kind != "PRIMARY")) throw new InvalidOperationException("La tabla no tiene clave primaria; no se generará una entidad mutable falsa.");
        if (keys.Single(x => x.Kind == "PRIMARY").Columns.Count != 1) throw new InvalidOperationException("La tabla usa una clave primaria compuesta. El generador exige una decisión explícita de identidad antes de crear una entidad.");
        var unsupported = columns.Select(x => (Column: x, Mapping: TypeMapper.Map(x))).Where(x => x.Mapping is null).Select(x => $"{x.Column.Name} ({x.Column.ColumnType})").ToArray();
        if (unsupported.Length > 0) throw new InvalidOperationException("Tipos MySQL sin mapeo explícito: " + string.Join(", ", unsupported));
        var duplicateProperties = columns.GroupBy(x => CodeGenerator.ToIdentifier(x.Name), StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicateProperties.Length > 0) throw new InvalidOperationException("Hay nombres de columna que producen propiedades C# duplicadas: " + string.Join(", ", duplicateProperties));
        const string foreignKeySql = "SELECT CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY CONSTRAINT_NAME, ORDINAL_POSITION";
        await using var fkCommand = new MySqlCommand(foreignKeySql, connection);
        fkCommand.Parameters.AddWithValue("@schema", schema);
        fkCommand.Parameters.AddWithValue("@table", table);
        var foreignKeys = new List<ForeignKeyInfo>();
        await using (var reader = await fkCommand.ExecuteReaderAsync())
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var index = foreignKeys.FindIndex(x => x.Name == name);
                if (index < 0) foreignKeys.Add(new(name, [reader.GetString(1)], reader.GetString(2), reader.GetString(3), [reader.GetString(4)]));
                else foreignKeys[index] = foreignKeys[index] with { Columns = foreignKeys[index].Columns.Append(reader.GetString(1)).ToArray(), ReferencedColumns = foreignKeys[index].ReferencedColumns.Append(reader.GetString(4)).ToArray() };
            }
        return new(schema, table, tableInfo.Kind, columns, keys) { ForeignKeys = foreignKeys };
    }
}

internal sealed record TypeMapping(string CsType, bool IsString = false);
internal static class TypeMapper
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

internal sealed record GeneratedFile(string Path, string Content);
internal static class CodeGenerator
{
    public static IReadOnlyList<GeneratedFile> Create(TableSchema schema, string? classOverride)
    {
        var className = classOverride ?? ToPascal(schema.Table);
        var ns = "OJunnySyS.Generated." + ToPascal(schema.Schema);
        var entity = RenderEntity(schema, className, ns);
        var dto = RenderDto(schema, className, ns);
        var config = RenderConfiguration(schema, className, ns);
        var guide = RenderGuide(schema, className);
        return [new($"Domain/{className}.cs", entity), new($"Application/{className}Dto.cs", dto), new($"Infrastructure/{className}Configuration.cs", config), new($"docs/{className}-integration.md", guide)];
    }

    private static string RenderEntity(TableSchema schema, string name, string ns)
    {
        var builder = new StringBuilder("using OJunnySyS.Domain.Common;\n\nnamespace ").Append(ns).AppendLine(";").AppendLine();
        var hasTenant = schema.Columns.Any(x => x.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase));
        var hasId = schema.PrimaryKey.Count == 1 && schema.PrimaryKey[0].Equals("Id", StringComparison.OrdinalIgnoreCase) && schema.Columns.Any(x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && TypeMapper.Map(x)?.CsType == "Guid");
        var hasCreated = schema.Columns.Any(x => x.Name.Equals("CreatedAtUtc", StringComparison.OrdinalIgnoreCase) && x.DataType is "datetime" or "timestamp");
        var inherits = hasTenant && hasId && hasCreated;
        builder.Append("public sealed class ").Append(name).Append(inherits ? " : TenantEntity\n{\n" : "\n{\n");
        builder.Append("    private ").Append(name).AppendLine("() { }").AppendLine();
        var constructorColumns = schema.Columns.Where(x => !IsBaseColumn(x, inherits) && !x.AutoIncrement).ToArray();
        if (constructorColumns.Length > 0)
        {
            var parameters = constructorColumns.Select(x => $"{TypeMapper.Map(x)!.CsType} {ToCamel(x.Name)}").ToList();
            if (inherits) parameters.Insert(0, "Guid tenantId");
            builder.Append("    public ").Append(name).Append('(').Append(string.Join(", ", parameters)).AppendLine(")" + (inherits ? " : base(tenantId)" : string.Empty)).AppendLine("    {");
            foreach (var column in constructorColumns) builder.Append("        ").Append(ToPascal(column.Name)).Append(" = ").Append(ToCamel(column.Name)).AppendLine(";");
            builder.AppendLine("    }").AppendLine();
        }
        foreach (var column in schema.Columns.Where(x => !IsBaseColumn(x, inherits)))
        {
            var type = TypeMapper.Map(column)!.CsType;
            builder.Append("    public ").Append(type).Append(' ').Append(ToPascal(column.Name)).Append(" { get; private set; }");
            if (type == "string") builder.Append(" = string.Empty;");
            else if (type == "byte[]") builder.Append(" = Array.Empty<byte>();");
            builder.AppendLine();
        }
        builder.AppendLine("\n    // TODO: agrega invariantes y métodos de negocio antes de utilizar esta entidad.").AppendLine("}");
        return builder.ToString();
    }

    private static bool IsBaseColumn(ColumnInfo c, bool inherits) => inherits && new[] { "id", "tenantid", "createdatutc", "updatedatutc" }.Contains(c.Name.ToLowerInvariant());

    private static string RenderDto(TableSchema schema, string name, string ns)
    {
        var b = new StringBuilder("namespace ").Append(ns).AppendLine(";").AppendLine().Append("public sealed record ").Append(name).AppendLine("Dto(");
        for (var i = 0; i < schema.Columns.Count; i++)
        {
            var c = schema.Columns[i];
            b.Append("    ").Append(TypeMapper.Map(c)!.CsType).Append(' ').Append(ToPascal(c.Name)).AppendLine(i == schema.Columns.Count - 1 ? ");" : ",");
        }
        return b.ToString();
    }

    private static string RenderConfiguration(TableSchema schema, string name, string ns)
    {
        var b = new StringBuilder("using Microsoft.EntityFrameworkCore;\nusing Microsoft.EntityFrameworkCore.Metadata.Builders;\n\nnamespace ").Append(ns).AppendLine(";").AppendLine().Append("public sealed class ").Append(name).Append("Configuration : IEntityTypeConfiguration<").Append(name).AppendLine(">").AppendLine("{").AppendLine("    public void Configure(EntityTypeBuilder<" + name + "> builder)").AppendLine("    {").Append("        builder.ToTable(").Append(ToCSharpLiteral(schema.Table)).AppendLine(");");
        var pk = schema.PrimaryKey;
        b.Append("        builder.HasKey(x => ").Append(pk.Count == 1 ? "x." + ToPascal(pk[0]) : "new { " + string.Join(", ", pk.Select(x => "x." + ToPascal(x))) + " }").AppendLine(");");
        foreach (var c in schema.Columns)
        {
            var prop = ToPascal(c.Name);
            b.Append("        builder.Property(x => x.").Append(prop).Append(").HasColumnName(").Append(ToCSharpLiteral(c.Name)).Append(')');
            if (!c.Nullable) b.Append(".IsRequired()");
            if (TypeMapper.Map(c)!.IsString && c.Length is > 0 and <= 4000) b.Append(".HasMaxLength(").Append(c.Length.Value).Append(')');
            if (c.DataType is "decimal" or "numeric" && c.Precision is > 0) b.Append(".HasPrecision(").Append(c.Precision.Value).Append(", ").Append(c.Scale ?? 0).Append(')');
            if (c.AutoIncrement) b.Append(".ValueGeneratedOnAdd()");
            b.AppendLine(";");
        }
        foreach (var key in schema.Keys.Where(x => x.Kind == "INDEX"))
        {
            var expression = key.Columns.Count == 1 ? "x => x." + ToPascal(key.Columns[0]) : "x => new { " + string.Join(", ", key.Columns.Select(x => "x." + ToPascal(x))) + " }";
            b.Append("        builder.HasIndex(").Append(expression).Append(")");
            if (key.Unique) b.Append(".IsUnique()");
            b.AppendLine(";");
        }
        b.AppendLine("    }").AppendLine("}");
        return b.ToString();
    }

    private static string RenderGuide(TableSchema schema, string name) => $$"""
# Integrar {{name}}

Tabla origen: `{{schema.Schema}}.{{schema.Table}}`. Archivos generados como punto de partida; revisa y adapta antes de integrarlos.

## Revisión requerida

- Confirma si la tabla pertenece a un tenant. El generador no adivina aislamiento: agrega el filtro global, las claves foráneas compuestas y los índices tenant-scoped según `docs/architecture.md`.
- Agrega `DbSet<{{name}}>` y aplica `{{name}}Configuration` en `StoreDbContext`.
- Define un contrato de repositorio, caso de uso y autorización en Application/API. Nunca expongas la entidad directamente.
- Agrega validaciones, invariantes, concurrencia y auditoría que requiera el dominio.
- Crea y revisa una migración EF Core; no uses SQL dinámico para persistencia.
- Mapea explícitamente relaciones y claves foráneas; valida los índices compuestos antes de publicar.
- Los DTO reflejan el esquema completo: elimina campos confidenciales o de escritura desde el cliente antes de utilizarlos en endpoints.

## Esquema leído

Clave primaria: `{{string.Join("`, `", schema.PrimaryKey)}}`.

Claves foráneas detectadas: {{(schema.ForeignKeys.Count == 0 ? "ninguna" : string.Join("; ", schema.ForeignKeys.Select(x => $"{string.Join(",", x.Columns)} → {x.ReferencedSchema}.{x.ReferencedTable}({string.Join(",", x.ReferencedColumns)})")))}}.

""";

    public static string ToIdentifier(string value) => ToPascal(value);

    private static string ToCSharpLiteral(string value) => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal) + "\"";

    private static string ToPascal(string value)
    {
        var chunks = Regex.Split(value, "[^A-Za-z0-9]+").Where(x => x.Length > 0);
        var result = string.Concat(chunks.Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
        if (result.Length == 0 || char.IsDigit(result[0])) result = "T" + result;
        return result;
    }

    private static string ToCamel(string value)
    {
        var pascal = ToPascal(value);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
