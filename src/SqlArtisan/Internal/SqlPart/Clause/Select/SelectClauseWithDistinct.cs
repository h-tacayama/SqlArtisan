namespace SqlArtisan.Internal;

internal sealed class SelectClauseWithDistinct : SqlPart
{
    // Either DISTINCT (DistinctKeyword) or DISTINCT ON (...) (DistinctOnKeyword);
    // both are SqlParts rendered as the select prefix.
    private readonly SqlPart _distinct;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithDistinct(
        SqlPart distinct,
        SqlPart[] selectItems)
    {
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClauseWithDistinct Parse(
        SqlPart distinct,
        object[] selectItems) => new(
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSpace(_distinct)
        .AppendSelectItems(_selectItems);
}
