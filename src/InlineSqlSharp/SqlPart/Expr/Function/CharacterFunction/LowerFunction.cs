namespace InlineSqlSharp;

public sealed class LowerFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal LowerFunction(AbstractExpr source)
    {
        _core = new UnaryFunctionCore(Keywords.LOWER, source);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
