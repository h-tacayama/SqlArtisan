namespace InlineSqlSharp;

internal sealed class GroupByItem(AbstractSqlPart item) : AbstractSqlPart
{
    private readonly AbstractSqlPart _item = item;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _item.FormatSql(buffer);
}
