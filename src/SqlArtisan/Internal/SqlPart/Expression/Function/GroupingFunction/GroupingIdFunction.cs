namespace SqlArtisan.Internal;

public sealed class GroupingIdFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GroupingIdFunction(SqlExpression[] args)
    {
        _core = new(Keywords.GroupingId, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
