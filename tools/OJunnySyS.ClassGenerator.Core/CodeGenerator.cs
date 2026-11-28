using System.Text;
using System.Text.RegularExpressions;

namespace OJunnySyS.ClassGenerator.Core;

public sealed record GeneratedFile(string Path, string Content);
public static class CodeGenerator
{
    private static readonly HashSet<string> CSharpKeywords = ["abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"];

    public static IReadOnlyList<GeneratedFile> Create(TableSchema schema, string? classOverride)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var className = classOverride ?? ToPascal(schema.Table);
        if (!IsValidClassName(className)) throw new ArgumentException("El nombre de clase debe ser un identificador C# válido y no puede ser palabra reservada.", nameof(classOverride));
        if (string.IsNullOrWhiteSpace(schema.Schema) || string.IsNullOrWhiteSpace(schema.Table)) throw new ArgumentException("El esquema debe incluir un nombre de base de datos y tabla.", nameof(schema));
        var ns = "OJunnySyS.Generated." + ToPascal(schema.Schema);
        var entity = RenderEntity(schema, className, ns);
        var dto = RenderDto(schema, className, ns);
        var config = RenderConfiguration(schema, className, ns);
        var guide = RenderGuide(schema, className);
        return [new($"Domain/{className}.cs", entity), new($"Application/{className}Dto.cs", dto), new($"Infrastructure/{className}Configuration.cs", config), new($"docs/{className}-integration.md", guide)];
    }

    public static bool IsValidClassName(string value) =>
        !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[A-Za-z_][A-Za-z0-9_]*$") && !CSharpKeywords.Contains(value);

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
