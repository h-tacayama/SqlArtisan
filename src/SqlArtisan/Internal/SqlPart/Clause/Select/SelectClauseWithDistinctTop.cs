namespace SqlArtisan.Internal;

internal sealed class SelectClauseWithDistinctTop : SqlPart, ITopSelectClause
{
    private readonly DistinctKeyword _distinct;
    private readonly TopClause _top;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithDistinctTop(
        DistinctKeyword distinct, TopClause top, SqlPart[] selectItems)
    {
        _distinct = distinct;
        _top = top;
        _selectItems = selectItems;
    }

    public bool WithTies => _top.HasWithTies;

    internal static SelectClauseWithDistinctTop Parse(
        DistinctKeyword distinct, TopClause top, object[] selectItems) =>
        new(distinct, top, SelectItemResolver.ResolveOrThrow(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSpace(_distinct)
        .AppendSpace(_top)
        .AppendSelectItems(_selectItems);
}
