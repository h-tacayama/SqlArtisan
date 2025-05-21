namespace SqlArtisan.Internal;

public sealed class DistinctKeyword : SqlPart
{
    internal DistinctKeyword() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Distinct);
}
