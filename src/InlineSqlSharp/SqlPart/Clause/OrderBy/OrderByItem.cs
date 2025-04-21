namespace InlineSqlSharp;

public sealed class OrderByItem(AbstractSqlPart item) : AbstractSqlPart
{
    private readonly AbstractSqlPart _item = item;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _item.FormatSql(buffer);
}
