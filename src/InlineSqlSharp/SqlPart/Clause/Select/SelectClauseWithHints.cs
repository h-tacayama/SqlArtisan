namespace InlineSqlSharp;

internal sealed class SelectClauseWithHints : AbstractSqlPart
{
    private readonly SqlHints _hints;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithHints(
        SqlHints hints,
        AbstractSqlPart[] selectItems)
    {
        _hints = hints;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithHints Parse(
        SqlHints hints,
        object[] selectItems) => new(
            hints,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.Select)
        .AppendSpaceIfNotNull(_hints)
        .AppendSelectItems(_selectItems);
}
