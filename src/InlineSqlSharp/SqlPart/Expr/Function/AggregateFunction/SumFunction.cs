namespace InlineSqlSharp;

public sealed class SumFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal SumFunction(AbstractExpr expr)
    {
        _core = new(Keywords.SUM, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
