namespace SqlArtisan.Internal;

internal sealed class DoNothingClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append($"{Keywords.Do} {Keywords.Nothing}");
}
