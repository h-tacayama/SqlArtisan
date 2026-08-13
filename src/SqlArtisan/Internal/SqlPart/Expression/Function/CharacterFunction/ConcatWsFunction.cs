namespace SqlArtisan.Internal;

public sealed class ConcatWsFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal ConcatWsFunction(SqlExpression[] args)
    {
        _core = new(Keywords.ConcatWs, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
