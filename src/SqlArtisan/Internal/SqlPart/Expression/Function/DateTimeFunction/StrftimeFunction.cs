namespace SqlArtisan.Internal;

public sealed class StrftimeFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal StrftimeFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Strftime, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
