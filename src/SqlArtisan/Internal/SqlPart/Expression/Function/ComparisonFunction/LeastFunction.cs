namespace SqlArtisan.Internal;

public sealed class LeastFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal LeastFunction(SqlExpression[] expressions)
    {
        CollectionGuard.ThrowIfEmpty(
            expressions,
            "LEAST requires at least one expression.");

        _core = new(Keywords.Least, expressions);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
