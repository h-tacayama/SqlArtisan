namespace SqlArtisan;

public sealed class LeastFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal LeastFunction(SqlExpression[] expressions)
    {
        _core = new(Keywords.Least, expressions);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
