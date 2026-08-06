namespace SqlArtisan.Internal;

public sealed class CoalesceFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal CoalesceFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Coalesce, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
