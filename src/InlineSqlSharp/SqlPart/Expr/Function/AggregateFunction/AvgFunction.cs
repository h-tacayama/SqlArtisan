namespace InlineSqlSharp;

public sealed class AvgFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal AvgFunction(AbstractExpr expr)
    {
        _core = new(Keywords.AVG, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
