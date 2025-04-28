namespace InlineSqlSharp;

public sealed class LeastFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal LeastFunction(AbstractExpr[] expressions)
    {
        _core = new(Keywords.Least, expressions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
