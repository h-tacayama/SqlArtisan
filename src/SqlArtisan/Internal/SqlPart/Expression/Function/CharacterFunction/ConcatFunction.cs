namespace SqlArtisan.Internal;

public sealed class ConcatFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal ConcatFunction(SqlExpression primary, SqlExpression secondary)
    {
        _core = new(Keywords.Concat, primary, secondary);
    }

    internal ConcatFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Concat, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
