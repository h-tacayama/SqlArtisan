namespace SqlArtisan.Internal;

internal sealed class ExceptOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Except)
        .PrependSpaceIfNotNull(_all ? Keywords.All : null);
}
