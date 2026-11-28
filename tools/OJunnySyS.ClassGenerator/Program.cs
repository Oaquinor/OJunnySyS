using System.Text;
using OJunnySyS.ClassGenerator.Core;
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
        if (className is not null && !CodeGenerator.IsValidClassName(className)) throw new ArgumentException("--class requiere un identificador C# válido que no sea palabra reservada.");
        if (command == "generate" && output is null) throw new ArgumentException("generate requiere --output.");
        return new(command, args[1], args[2], className, output);
    }
}
