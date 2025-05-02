namespace SqlArtisan;

public sealed class GreatestFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GreatestFunction(SqlExpression[] expressions)
    {
        _core = new(Keywords.Greatest, expressions);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
