namespace SqlArtisan.Internal;

public sealed class ConcatFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal ConcatFunction(
        SqlExpression primary,
        SqlExpression secondary,
        SqlExpression[] others)
    {
        _core = new(Keywords.Concat, [primary, secondary, .. others]);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
