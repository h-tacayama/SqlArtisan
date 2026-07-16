namespace SqlArtisan.Internal;

internal sealed class SelectClauseWithTop : SqlPart, ITopSelectClause
{
    private readonly TopClause _top;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithTop(TopClause top, SqlPart[] selectItems)
    {
        _top = top;
        _selectItems = selectItems;
    }

    public bool WithTies => _top.HasWithTies;

    internal static SelectClauseWithTop Parse(TopClause top, object[] selectItems) =>
        new(top, SelectItemResolver.ResolveOrThrow(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSpace(_top)
        .AppendSelectItems(_selectItems);
}
