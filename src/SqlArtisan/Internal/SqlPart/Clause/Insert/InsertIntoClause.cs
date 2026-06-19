namespace SqlArtisan.Internal;

internal sealed class InsertIntoClause(DbTableBase table, DbColumn[] columns) : SqlPart
{
    private readonly DbTableBase _table = table;
    private readonly DbColumn[] _columns = columns;

    internal InsertIntoClause(DbTableBase table)
        : this(table, [])
    {
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Insert} {Keywords.Into} ");
        _table.FormatAsDmlTarget(buffer);

        if (_columns.Length > 0)
        {
            buffer.AppendSpace()
                .OpenParenthesis()
                .AppendColumnNamesCsv(_columns)
                .CloseParenthesis();
        }
    }
}
