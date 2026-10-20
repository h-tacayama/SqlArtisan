using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// The <c>DISTINCT</c> marker <see cref="Sql.Distinct"/> passes to <c>Select</c> and
/// the aggregates that take it.
/// </summary>
public sealed class DistinctKeyword : SqlPart
{
    internal DistinctKeyword() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Distinct);
}
