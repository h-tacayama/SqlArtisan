namespace InlineSqlSharp;

internal sealed class UpdateClause(AbstractTable table) : ISqlElement
{
    private readonly AbstractTable _table = table;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.UPDATE)
        .Append(_table);
}
