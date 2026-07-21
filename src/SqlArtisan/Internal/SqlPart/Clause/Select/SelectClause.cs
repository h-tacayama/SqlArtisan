namespace SqlArtisan.Internal;

internal sealed class SelectClause : SqlPart, ISelectItemsClause
{
    private readonly SqlPart[] _selectItems;

    private SelectClause(SqlPart[] selectItems)
    {
        _selectItems = selectItems;
    }

    public SqlPart[] SelectItems => _selectItems;

    internal static SelectClause Parse(object[] selectItems) =>
        new(SelectItemResolver.ResolveOrThrow(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSelectItems(_selectItems);
}
