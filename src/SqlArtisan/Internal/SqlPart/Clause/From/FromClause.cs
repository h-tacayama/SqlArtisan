namespace SqlArtisan.Internal;

internal sealed class FromClause : SqlPart
{
    private readonly TableReference[] _tables;

    internal const string NullTableMessage =
        "A FROM clause must not contain a null table reference.";

    internal FromClause(TableReference[] tables)
    {
        CollectionGuard.ThrowIfNullElement(tables, nameof(tables), NullTableMessage);
        _tables = tables;
    }

    internal TableReference[] Tables => _tables;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.From} ")
        .AppendCsv(_tables);
}
