using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Ri.MssqlMcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var settings = builder.Configuration.GetSection("Mcp").Get<McpSettings>() ?? new McpSettings();
var connections = builder.Configuration.GetSection("Connections").Get<List<ConnectionProfile>>() ?? [];
if (connections.Count == 0 || connections.Any(c => string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.ConnectionString)))
    throw new InvalidOperationException("Configure at least one named SQL Server connection using environment variables or appsettings.json.");

SqlServerTools.Initialize(new SqlServerRepository(connections, settings));
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
