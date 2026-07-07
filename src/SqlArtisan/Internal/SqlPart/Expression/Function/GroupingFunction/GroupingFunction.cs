namespace SqlArtisan.Internal;

public sealed class GroupingFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GroupingFunction(SqlExpression expr)
    {
        _core = new(Keywords.Grouping, expr);
    }

    internal GroupingFunction(SqlExpression expr1, SqlExpression expr2, SqlExpression[] others)
    {
        _core = new(Keywords.Grouping, [expr1, expr2, .. others]);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
