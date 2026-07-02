namespace SqlArtisan.Internal;

internal sealed class SelectClauseWithOptions : SqlPart
{
    private readonly SqlHints _hints;
    // Either DISTINCT (DistinctKeyword) or DISTINCT ON (...) (DistinctOnKeyword).
    private readonly SqlPart _distinct;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithOptions(
        SqlHints hints,
        SqlPart distinct,
        SqlPart[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithOptions Parse(
        SqlHints hints,
        SqlPart distinct,
        object[] selectItems) => new(
            hints,
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSpace(_hints)
        .AppendSpace(_distinct)
        .AppendSelectItems(_selectItems);
}
