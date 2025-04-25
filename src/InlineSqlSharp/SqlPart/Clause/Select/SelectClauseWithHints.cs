namespace InlineSqlSharp;

internal sealed class SelectClauseWithHints : AbstractSqlPart
{
    private readonly Hints _hints;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClauseWithHints(
        Hints hints,
        AbstractSqlPart[] selectItems)
    {
        _hints = hints;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithHints Parse(
        Hints hints,
        object[] selectItems) => new(
            hints,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIfNotNull(_hints)
        .AppendSelectItems(_selectItems);
}
