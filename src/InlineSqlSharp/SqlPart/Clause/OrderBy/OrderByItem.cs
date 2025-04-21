namespace InlineSqlSharp;

public sealed class OrderByItem : AbstractSqlPart
{
    private readonly AbstractSqlPart _item;

    internal OrderByItem(AbstractSqlPart item)
    {
        _item = item;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _item.FormatSql(buffer);
}
