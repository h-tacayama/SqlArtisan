namespace SqlArtisan.Internal;

internal sealed class SelectClauseWithHintsDistinct : SqlPart, ISelectItemsClause
{
    private readonly SqlHints _hints;
    private readonly SqlPart _distinct;
    private readonly SqlPart[] _selectItems;

    private SelectClauseWithHintsDistinct(SqlHints hints, SqlPart distinct, SqlPart[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectItems = selectItems;
    }

    public SqlPart[] SelectItems => _selectItems;

    internal static SelectClauseWithHintsDistinct Parse(
        SqlHints hints,
        SqlPart distinct,
        object[] selectItems) => new(
            hints,
            distinct,
            SelectItemResolver.ResolveNonEmpty(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .Append(_hints)
        .AppendSpace(_distinct)
        .AppendSelectItems(_selectItems);
}
