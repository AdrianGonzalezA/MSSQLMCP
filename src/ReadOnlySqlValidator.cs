using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;

namespace Ri.MssqlMcp;

internal static class ReadOnlySqlValidator
{
    public static void Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("La consulta está vacía.");

        // Parse as modern T-SQL. A parsed AST avoids keyword checks that can be
        // bypassed by comments, quoted strings, or nested queries.
        TSqlParser parser = new TSql160Parser(initialQuotedIdentifiers: true);
        var fragment = parser.Parse(new StringReader(sql), out IList<ParseError> errors);
        if (errors.Count != 0 || fragment is not TSqlScript script || script.Batches.Count != 1 || script.Batches[0].Statements.Count != 1)
            throw new ArgumentException("Se admite una única sentencia T-SQL válida.");

        if (script.Batches[0].Statements[0] is not SelectStatement select)
            throw new ArgumentException("Solo se permiten sentencias SELECT.");

        if (select.Into is not null)
            throw new ArgumentException("SELECT INTO no está permitido.");
    }
}
