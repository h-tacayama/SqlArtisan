namespace InlineSqlSharp;

internal sealed class SelectClauseWithDistinct : AbstractSqlPart
{
    private readonly DistinctKeyword _distinct;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithDistinct(
        DistinctKeyword distinct,
        AbstractSqlPart[] selectItems)
    {
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithDistinct Parse(
        DistinctKeyword distinct,
        object[] selectItems) => new(
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Select)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectItems(_selectItems);
}
