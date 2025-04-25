namespace InlineSqlSharp;

internal sealed class SelectClauseWithOptions : AbstractSqlPart
{
    private readonly Hints _hints;
    private readonly Distinct _distinct;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithOptions(
        Hints hints,
        Distinct distinct,
        AbstractSqlPart[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithOptions Parse(
        Hints hints,
        Distinct distinct,
        object[] selectItems) => new(
            hints,
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIfNotNull(_hints)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectItems(_selectItems);
}
