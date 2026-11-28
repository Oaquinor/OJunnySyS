using MySql.Data.MySqlClient;

namespace OJunnySyS.ClassGenerator.Core;

public static class SchemaReader
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
