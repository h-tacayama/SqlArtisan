namespace SqlArtisan.Internal;

public sealed class GroupingFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GroupingFunction(SqlExpression expr)
    {
        _core = new(Keywords.Grouping, expr);
    }

    internal GroupingFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Grouping, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
