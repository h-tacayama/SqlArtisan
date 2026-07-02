namespace SqlArtisan.Internal;

internal sealed class MinusOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Minus)
        .PrependSpaceIfNotNull(_all ? Keywords.All : null);
}
