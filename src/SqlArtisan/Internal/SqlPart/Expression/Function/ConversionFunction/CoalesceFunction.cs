namespace SqlArtisan.Internal;

public sealed class CoalesceFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal CoalesceFunction(
        SqlExpression primary,
        SqlExpression secondary,
        SqlExpression[] others)
    {
        _core = new(Keywords.Coalesce, [primary, secondary, .. others]);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
