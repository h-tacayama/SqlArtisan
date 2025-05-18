namespace SqlArtisan;

internal sealed class InsertIntoClause(DbTableBase table, DbColumn[] columns) : SqlPart
{
    private readonly DbTableBase _table = table;
    private readonly DbColumn[] _columns = columns;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append($"{Keywords.Insert} {Keywords.Into} ")
            .Append(_table);

        if (_columns.Length > 0)
        {
            buffer.Append(" ")
                .OpenParenthesis()
                .AppendCsv(_columns)
                .CloseParenthesis();
        }
    }
}
