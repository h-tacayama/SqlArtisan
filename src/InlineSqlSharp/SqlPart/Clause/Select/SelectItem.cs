namespace InlineSqlSharp;

internal sealed class SelectItem(AbstractSqlPart item) : AbstractSqlPart
{
    private readonly AbstractSqlPart _item = item;

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        if (_item is ExprAlias alias)
        {
            alias.FormatAsSelect(buffer);
        }
        else
        {
            _item.FormatSql(buffer);
        }
    }
}
