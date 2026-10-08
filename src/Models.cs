namespace Ri.MssqlMcp;

public sealed class McpSettings
{
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRows { get; set; } = 500;
    public int MaxColumns { get; set; } = 100;
}

public sealed class ConnectionProfile
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
