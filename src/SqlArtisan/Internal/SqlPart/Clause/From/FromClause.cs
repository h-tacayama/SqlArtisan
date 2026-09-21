namespace SqlArtisan.Internal;

internal sealed class FromClause : SqlPart
{
    private readonly TableReference[] _tables;

    internal FromClause(TableReference[] tables)
    {
        CollectionGuard.ThrowIfNullElement(
            tables, nameof(tables), "A FROM clause must not contain a null table reference.");
        _tables = tables;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.From} ")
        .AppendCsv(_tables);
}
