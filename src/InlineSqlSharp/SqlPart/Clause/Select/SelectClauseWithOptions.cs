namespace InlineSqlSharp;

internal sealed class SelectClauseWithOptions : AbstractSqlPart
{
    private readonly SqlHints _hints;
    private readonly DistinctKeyword _distinct;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithOptions(
        SqlHints hints,
        DistinctKeyword distinct,
        AbstractSqlPart[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithOptions Parse(
        SqlHints hints,
        DistinctKeyword distinct,
        object[] selectItems) => new(
            hints,
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Select)
        .AppendSpaceIfNotNull(_hints)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectItems(_selectItems);
}
