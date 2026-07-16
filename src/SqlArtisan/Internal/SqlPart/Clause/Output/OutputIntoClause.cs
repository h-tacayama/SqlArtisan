namespace SqlArtisan.Internal;

// The `INTO table (cols)` redirect of a SQL Server OUTPUT clause, emitted as a
// separate part right after the OUTPUT part so space-joining yields
// `OUTPUT ... INTO table (cols)`.
internal sealed class OutputIntoClause(DbTableBase table, DbColumn[] columns) : SqlPart
{
    private readonly DbTableBase _table = table;
    private readonly DbColumn[] _columns = columns;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Into} ");
        _table.FormatAsDmlTarget(buffer);

        if (_columns.Length > 0)
        {
            buffer.AppendSpace()
                .OpenParenthesis()
                .AppendUnqualifiedColumnsCsv(_columns)
                .CloseParenthesis();
        }
    }
}
