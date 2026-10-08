using ModelContextProtocol.Client;
using System.Text.Json;
using Xunit;

namespace MssqlMcp.Tests;

public sealed class McpServerIntegrationTests
{
    [Fact]
    public async Task Server_exposes_expected_tools_and_lists_configured_connections()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var serverDll = Path.Combine(AppContext.BaseDirectory, "MssqlMcp.dll");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "ri-mssql-test",
            Command = "dotnet",
            Arguments = [serverDll],
            WorkingDirectory = projectRoot,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["Connections__0__Name"] = "principal",
                ["Connections__0__ConnectionString"] = "Server=127.0.0.1,1;Database=unused;User Id=unused;Password=unused;Encrypt=False;Connect Timeout=1"
            }
        });

        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();
        Assert.Equal(
            ["describe_table", "list_connections", "list_tables", "query"],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal).ToArray());

        var result = await client.CallToolAsync("list_connections");
        Assert.NotEqual(true, result.IsError);
        Assert.Contains("principal", JsonSerializer.Serialize(result));
    }
}
