namespace SqlArtisan;

internal sealed class SelectClauseWithOptions : SqlPart
{
    private readonly SqlHints _hints;
    private readonly DistinctKeyword _distinct;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithOptions(
        SqlHints hints,
        DistinctKeyword distinct,
        SqlPart[] selectItems)
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
        .Append($"{Keywords.Select} ")
        .AppendSpace(_hints)
        .AppendSpace(_distinct)
        .AppendSelectItems(_selectItems);
}
