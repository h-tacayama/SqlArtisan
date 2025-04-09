namespace InlineSqlSharp;

internal sealed class DeleteClause(AbstractTable table) : ISqlElement
{
    private readonly AbstractTable _table = table;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.DELETE)
        .AppendSpace(Keywords.FROM)
        .Append(_table);
}
