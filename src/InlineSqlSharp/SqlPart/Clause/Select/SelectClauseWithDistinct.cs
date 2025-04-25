namespace InlineSqlSharp;

internal sealed class SelectClauseWithDistinct : AbstractSqlPart
{
    private readonly Distinct _distinct;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithDistinct(
        Distinct distinct,
        AbstractSqlPart[] selectItems)
    {
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithDistinct Parse(
        Distinct distinct,
        object[] selectItems) => new(
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectItems(_selectItems);
}
