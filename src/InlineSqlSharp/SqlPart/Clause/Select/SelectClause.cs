namespace InlineSqlSharp;

internal sealed class SelectClause : AbstractSqlPart
{
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClause(AbstractSqlPart[] selectItems)
    {
        _selectItems = selectItems;
    }

    internal static SelectClause Parse(object[] selectItems) =>
        new(SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSelectItems(_selectItems);
}
