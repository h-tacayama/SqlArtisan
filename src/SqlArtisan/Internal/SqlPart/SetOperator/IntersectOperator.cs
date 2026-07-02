namespace SqlArtisan.Internal;

internal sealed class IntersectOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Intersect)
        .PrependSpaceIfNotNull(_all ? Keywords.All : null);
}
