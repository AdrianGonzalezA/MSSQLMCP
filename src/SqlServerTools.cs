using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Ri.MssqlMcp;

[McpServerToolType]
public static class SqlServerTools
{
    private static SqlServerRepository? _repository;
    internal static void Initialize(SqlServerRepository repository) => _repository = repository;
    private static SqlServerRepository Repository => _repository ?? throw new InvalidOperationException("MCP no inicializado.");

    [McpServerTool, Description("Lista los perfiles de SQL Server configurados. No revela credenciales ni cadenas de conexión.")]
    public static object ListConnections() => Repository.ListConnections();

    [McpServerTool, Description("Lista tablas y vistas de usuario del perfil de base de datos indicado.")]
    public static Task<string> ListTables(string connection, CancellationToken cancellationToken = default) => Repository.ListTablesAsync(connection, cancellationToken);

    [McpServerTool, Description("Devuelve columnas y tipos de una tabla. Indica esquema y tabla por separado, por ejemplo dbo y Clientes.")]
    public static Task<string> DescribeTable(string connection, string schema, string table, CancellationToken cancellationToken = default) => Repository.DescribeTableAsync(connection, schema, table, cancellationToken);

    [McpServerTool, Description("Ejecuta una única consulta T-SQL SELECT de solo lectura. Devuelve hasta MaxRows. Usa parámetros literales seguros en la consulta; no envíes INSERT, UPDATE, DELETE, EXEC, procedimientos ni SELECT INTO.")]
    public static Task<string> Query(string connection, string sql, CancellationToken cancellationToken = default) => Repository.ExecuteSelectAsync(connection, sql, cancellationToken);
}
