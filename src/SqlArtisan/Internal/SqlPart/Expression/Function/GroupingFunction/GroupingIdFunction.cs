namespace SqlArtisan.Internal;

public sealed class GroupingIdFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GroupingIdFunction(SqlExpression expr, SqlExpression[] others)
    {
        _core = new(Keywords.GroupingId, [expr, .. others]);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
