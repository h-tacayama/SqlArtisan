namespace SqlArtisan.Internal;

// The standalone `DELETE` action of a MERGE WHEN clause (SQL Server).
internal sealed class MergeDeleteClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Delete);
}
