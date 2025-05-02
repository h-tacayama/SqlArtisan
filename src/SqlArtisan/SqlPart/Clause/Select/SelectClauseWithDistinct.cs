namespace SqlArtisan;

internal sealed class SelectClauseWithDistinct : SqlPart
{
    private readonly DistinctKeyword _distinct;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithDistinct(
        DistinctKeyword distinct,
        SqlPart[] selectItems)
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
        .Append($"{Keywords.Select} ")
        .AppendSpace(_distinct)
        .AppendSelectItems(_selectItems);
}
