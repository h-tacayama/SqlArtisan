namespace SqlArtisan.Internal;

// PostgreSQL's `DELETE FROM target USING a, b WHERE ...` source list. Distinct
// from MERGE's single-source `MergeUsingClause` — here USING takes a comma list.
internal sealed class DeleteUsingClause(TableReference[] tables) : SqlPart
{
    private readonly TableReference[] _tables = tables;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .AppendCsv(_tables);
}
