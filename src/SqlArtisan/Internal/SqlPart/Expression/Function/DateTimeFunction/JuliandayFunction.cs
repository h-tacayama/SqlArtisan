namespace SqlArtisan.Internal;

public sealed class JuliandayFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal JuliandayFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Julianday, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
