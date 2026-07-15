namespace SqlArtisan.Internal;

// The `INTO archive (cols)` redirect of a SQL Server OUTPUT clause, emitted as a
// separate part right after the OUTPUT part so space-joining yields
// `OUTPUT ... INTO archive (cols)`.
internal sealed class OutputIntoClause(DbTableBase archive, DbColumn[] columns) : SqlPart
{
    private readonly DbTableBase _archive = archive;
    private readonly DbColumn[] _columns = columns;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Into} ");
        _archive.FormatAsDmlTarget(buffer);

        if (_columns.Length > 0)
        {
            buffer.AppendSpace()
                .OpenParenthesis()
                .AppendUnqualifiedColumnsCsv(_columns)
                .CloseParenthesis();
        }
    }
}
