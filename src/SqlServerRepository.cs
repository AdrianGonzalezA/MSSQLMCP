using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace Ri.MssqlMcp;

internal sealed class SqlServerRepository
{
    private readonly IReadOnlyDictionary<string, string> _connections;
    private readonly McpSettings _settings;

    public SqlServerRepository(IEnumerable<ConnectionProfile> profiles, McpSettings settings)
    {
        _connections = profiles.ToDictionary(p => p.Name, p => Harden(p.ConnectionString), StringComparer.OrdinalIgnoreCase);
        _settings = settings;
    }

    public object ListConnections() => _connections.Keys.Order(StringComparer.OrdinalIgnoreCase).Select(name => new { name }).ToArray();

    public async Task<string> ListTablesAsync(string connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT s.name AS schema_name, t.name AS table_name, 'TABLE' AS object_type FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE t.is_ms_shipped = 0 UNION ALL SELECT s.name, v.name, 'VIEW' FROM sys.views v JOIN sys.schemas s ON s.schema_id = v.schema_id WHERE v.is_ms_shipped = 0 ORDER BY schema_name, table_name";
        return await QueryAsync(connection, sql, [], cancellationToken);
    }

    public async Task<string> DescribeTableAsync(string connection, string schema, string table, CancellationToken cancellationToken)
    {
        const string sql = "SELECT c.name AS column_name, ty.name AS data_type, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id JOIN sys.schemas s ON s.schema_id = t.schema_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE s.name = @schema AND t.name = @table ORDER BY c.column_id";
        return await QueryAsync(connection, sql, new[] { new SqlParameter("@schema", schema), new SqlParameter("@table", table) }, cancellationToken);
    }

    public async Task<string> ExecuteSelectAsync(string connection, string sql, CancellationToken cancellationToken)
    {
        ReadOnlySqlValidator.Validate(sql);
        return await QueryAsync(connection, sql, [], cancellationToken);
    }

    private async Task<string> QueryAsync(string profile, string sql, SqlParameter[] parameters, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(profile, out var connectionString))
            throw new ArgumentException($"Conexión desconocida: {profile}. Usa list_connections para ver las disponibles.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = Math.Clamp(_settings.CommandTimeoutSeconds, 1, 300)
        };
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        var columnCount = Math.Min(reader.FieldCount, Math.Clamp(_settings.MaxColumns, 1, 500));
        var columns = Enumerable.Range(0, columnCount).Select(reader.GetName).ToArray();
        var rows = new List<Dictionary<string, object?>>();
        var maxRows = Math.Clamp(_settings.MaxRows, 1, 10000);
        while (rows.Count < maxRows && await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columnCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        var truncated = rows.Count == maxRows && await reader.ReadAsync(cancellationToken);
        return JsonSerializer.Serialize(new { columns, rows, returned_rows = rows.Count, truncated });
    }

    private static string Harden(string raw)
    {
        var builder = new SqlConnectionStringBuilder(raw)
        {
            PersistSecurityInfo = false,
            ApplicationName = "RI MSSQL MCP"
        };
        // SqlClient 7.x supports enum-backed Encrypt; assigning by keyword keeps
        // the source compatible with the current connection-string contract.
        if (!builder.ContainsKey("Encrypt")) builder["Encrypt"] = "True";
        if (!builder.ContainsKey("TrustServerCertificate")) builder["TrustServerCertificate"] = "False";
        return builder.ConnectionString;
    }
}
