namespace SqlArtisan.Internal;

// PostgreSQL's `DELETE FROM target USING a, b WHERE ...` source list. Distinct
// from MERGE's single-source `MergeUsingClause` — here USING takes a comma list.
internal sealed class DeleteUsingClause : SqlPart
{
    private readonly TableReference[] _tables;

    internal DeleteUsingClause(TableReference[] tables)
    {
        CollectionGuard.ThrowIfNullElement(
            tables, nameof(tables), "A USING clause must not contain a null table reference.");
        _tables = tables;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .AppendCsv(_tables);
}
