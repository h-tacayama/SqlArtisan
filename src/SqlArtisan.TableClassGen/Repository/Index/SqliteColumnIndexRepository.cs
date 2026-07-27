using System.Data;

namespace SqlArtisan.TableClassGen;

// pragma_index_info marks an expression column with cid -2 and a null name, and
// carries no text for it — the expression is only in the index's DDL, so the two
// pragmas locate the indexes and sqlite_master supplies what to scan.
internal sealed class SqliteColumnIndexRepository : IColumnIndexRepository
{
    public ColumnIndexInfo Read(IDbConnection conn, string tableName)
    {
        List<string> indexNames = [];
        using (IDbCommand command = conn.CreateCommand())
        {
            command.CommandText = "SELECT name FROM pragma_index_list(@table)";
            AddParameter(command, "@table", tableName);

            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                indexNames.Add(reader.GetString(0));
            }
        }

        List<string> leadingColumns = [];
        List<string> expressionIndexNames = [];

        foreach (string indexName in indexNames)
        {
            using IDbCommand command = conn.CreateCommand();
            command.CommandText = "SELECT seqno, name FROM pragma_index_info(@index)";
            AddParameter(command, "@index", indexName);

            using IDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(1))
                {
                    expressionIndexNames.Add(indexName);
                }
                else if (reader.GetInt32(0) == 0)
                {
                    leadingColumns.Add(reader.GetString(1));
                }
            }
        }

        return new ColumnIndexInfo(leadingColumns, ExpressionTexts(conn, expressionIndexNames));
    }

    // Only the expression-bearing indexes are scanned: a plain index's DDL names
    // its own column, which would mark every indexed column unknown.
    private static List<string> ExpressionTexts(IDbConnection conn, List<string> indexNames)
    {
        List<string> texts = [];

        foreach (string indexName in indexNames)
        {
            using IDbCommand command = conn.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = @name";
            AddParameter(command, "@name", indexName);

            if (command.ExecuteScalar() is string sql)
            {
                texts.Add(sql);
            }
        }

        return texts;
    }

    private static void AddParameter(IDbCommand command, string name, string value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
