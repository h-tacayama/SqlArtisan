namespace SqlArtisan.Internal;

public sealed class GreatestFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal GreatestFunction(SqlExpression[] expressions)
    {
        CollectionGuard.ThrowIfEmpty(
            expressions,
            "GREATEST requires at least one expression.");

        _core = new(Keywords.Greatest, expressions);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
