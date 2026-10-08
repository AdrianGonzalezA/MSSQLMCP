using Microsoft.Data.SqlClient;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Text.Json;

var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var envPath = Path.Combine(projectRoot, ".env");
var profile = ReadEnvValue(envPath, "MSSQL_PROFILE_NAME");
var connectionString = ReadEnvValue(envPath, "MSSQL_CONNECTION_STRING");
var serverErrors = new ConcurrentQueue<string>();
var publishedExecutable = args.FirstOrDefault();
var command = publishedExecutable is null ? "dotnet" : Path.GetFullPath(publishedExecutable);
var commandArguments = publishedExecutable is null
    ? new[] { Path.Combine(AppContext.BaseDirectory, "MssqlMcp.dll") }
    : Array.Empty<string>();

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "ri-mssql-probe",
    Command = command,
    Arguments = commandArguments,
    WorkingDirectory = projectRoot,
    EnvironmentVariables = new Dictionary<string, string?>
    {
        ["Connections__0__Name"] = profile,
        ["Connections__0__ConnectionString"] = connectionString,
        ["Mcp__CommandTimeoutSeconds"] = ReadOptionalEnvValue(envPath, "MCP_TIMEOUT_SECONDS") ?? "30",
        ["Mcp__MaxRows"] = ReadOptionalEnvValue(envPath, "MCP_MAX_ROWS") ?? "500"
    },
    StandardErrorLines = serverErrors.Enqueue
});

await using var client = await McpClient.CreateAsync(transport);

var tools = await client.ListToolsAsync();
Console.WriteLine($"MCP OK: {tools.Count} herramientas ({string.Join(", ", tools.Select(tool => tool.Name).Order())})");

var connectionsResult = await client.CallToolAsync("list_connections");
EnsureSuccess("list_connections", connectionsResult);
Console.WriteLine($"Perfil configurado: {profile}");

CallToolResult queryResult;
try
{
    queryResult = await client.CallToolAsync("query", new Dictionary<string, object?>
    {
        ["connection"] = profile,
        ["sql"] = "SELECT DB_NAME() AS database_name, CAST(1 AS int) AS connection_ok"
    });
}
catch (Exception exception)
{
    Console.Error.WriteLine($"La llamada MCP llegó al servidor pero la consulta falló: {exception.GetType().Name}.");
    foreach (var line in serverErrors.TakeLast(50))
        Console.Error.WriteLine(RedactSecrets(line));
    await DiagnoseConnection(connectionString);
    Environment.ExitCode = 1;
    return;
}

static string RedactSecrets(string value) =>
    Regex.Replace(value, "(?i)(Password|Pwd)\\s*=\\s*[^;\\s]+", "$1=*****");
var queryJson = GetText("query", queryResult);
using (var queryDocument = JsonDocument.Parse(queryJson))
{
    var row = queryDocument.RootElement.GetProperty("rows")[0];
    Console.WriteLine($"SQL Server OK: base={row.GetProperty("database_name").GetString()}, connection_ok={row.GetProperty("connection_ok").GetInt32()}");
}

var tablesResult = await client.CallToolAsync("list_tables", new Dictionary<string, object?>
{
    ["connection"] = profile
});
var tablesJson = GetText("list_tables", tablesResult);
using (var tablesDocument = JsonDocument.Parse(tablesJson))
{
    var root = tablesDocument.RootElement;
    var rows = root.GetProperty("rows");
    Console.WriteLine($"Objetos visibles: {root.GetProperty("returned_rows").GetInt32()}; truncado={root.GetProperty("truncated").GetBoolean()}");

    var firstTable = rows.EnumerateArray().FirstOrDefault(row => row.GetProperty("object_type").GetString() == "TABLE");
    if (firstTable.ValueKind != JsonValueKind.Undefined)
    {
        var schema = firstTable.GetProperty("schema_name").GetString()!;
        var table = firstTable.GetProperty("table_name").GetString()!;
        var describeResult = await client.CallToolAsync("describe_table", new Dictionary<string, object?>
        {
            ["connection"] = profile,
            ["schema"] = schema,
            ["table"] = table
        });
        var describeJson = GetText("describe_table", describeResult);
        using var describeDocument = JsonDocument.Parse(describeJson);
        Console.WriteLine($"Descripción OK: {schema}.{table}, columnas={describeDocument.RootElement.GetProperty("returned_rows").GetInt32()}");
    }
}

static string GetText(string tool, CallToolResult result)
{
    EnsureSuccess(tool, result);
    return result.Content.OfType<TextContentBlock>().Single().Text;
}

static void EnsureSuccess(string tool, CallToolResult result)
{
    if (result.IsError == true)
        throw new InvalidOperationException($"La herramienta {tool} devolvió un error: {JsonSerializer.Serialize(result)}");
}

static string ReadEnvValue(string path, string key)
{
    if (!File.Exists(path))
        throw new InvalidOperationException($"No existe {path}.");

    var prefix = key + "=";
    var line = File.ReadLines(path).FirstOrDefault(line => line.TrimStart().StartsWith(prefix, StringComparison.Ordinal));
    if (line is null)
        throw new InvalidOperationException($"Falta {key} en {path}.");

    var value = line.TrimStart()[prefix.Length..].Trim();
    if (value.Length >= 2 && ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"')))
        value = value[1..^1];

    return value;
}

static string? ReadOptionalEnvValue(string path, string key)
{
    var prefix = key + "=";
    var line = File.ReadLines(path).FirstOrDefault(line => line.TrimStart().StartsWith(prefix, StringComparison.Ordinal));
    if (line is null)
        return null;

    var value = line.TrimStart()[prefix.Length..].Trim();
    if (value.Length >= 2 && ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"')))
        value = value[1..^1];

    return value;
}

static async Task DiagnoseConnection(string connectionString)
{
    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        Console.Error.WriteLine("La conexión directa abrió correctamente; el error está en la ejecución MCP.");
    }
    catch (SqlException exception)
    {
        var message = exception.Message.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)[0];
        Console.Error.WriteLine($"SQL Server rechazó la conexión: número={exception.Number}, estado={exception.State}, clase={exception.Class}.");
        Console.Error.WriteLine(message);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"No se pudo abrir la conexión: {exception.GetType().Name}: {exception.Message}");
    }
}
