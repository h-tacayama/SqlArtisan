namespace SqlArtisan.Internal;

// The `INTO table (cols)` redirect of a SQL Server OUTPUT clause, emitted as a
// separate part right after the OUTPUT part so space-joining yields
// `OUTPUT ... INTO table (cols)`.
internal sealed class OutputIntoClause : SqlPart
{
    private readonly DbTableBase _table;
    private readonly DbColumn[] _columns;

    internal OutputIntoClause(DbTableBase table, DbColumn[] columns)
    {
        DmlTargetGuard.ThrowIfOutputIntoTargetAliased(table);
        CollectionGuard.ThrowIfNullElement(
            columns, nameof(columns), "An OUTPUT INTO column list must not contain a null column.");
        ColumnListGuard.ThrowIfDuplicate(
            columns, "An OUTPUT INTO column list must not name a column twice.");
        _table = table;
        _columns = columns;
    }

    // Read by OutputClauseGuard: zero means the positional form, which the
    // engine widths against the table, not the OUTPUT list.
    internal int ColumnCount => _columns.Length;

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
