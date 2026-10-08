using Ri.MssqlMcp;
using Xunit;

namespace MssqlMcp.Tests;

public sealed class ReadOnlySqlValidatorTests
{
    [Theory]
    [InlineData("SELECT 1")]
    [InlineData("WITH datos AS (SELECT 1 AS id) SELECT id FROM datos")]
    [InlineData("SELECT 'DELETE FROM dbo.Clientes' AS texto")]
    public void Validate_accepts_single_read_only_select(string sql)
    {
        ReadOnlySqlValidator.Validate(sql);
    }

    [Theory]
    [InlineData("")]
    [InlineData("DELETE FROM dbo.Clientes")]
    [InlineData("EXEC dbo.Procesar")]
    [InlineData("SELECT 1; SELECT 2")]
    [InlineData("SELECT * INTO dbo.Copia FROM dbo.Clientes")]
    public void Validate_rejects_non_read_only_or_multiple_statements(string sql)
    {
        Assert.Throws<ArgumentException>(() => ReadOnlySqlValidator.Validate(sql));
    }
}
