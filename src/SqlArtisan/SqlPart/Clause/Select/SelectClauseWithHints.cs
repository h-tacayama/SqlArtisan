namespace SqlArtisan;

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
        .Append($"{Keywords.Select} ")
        .AppendSpace(_hints)
        .AppendSelectItems(_selectItems);
}
