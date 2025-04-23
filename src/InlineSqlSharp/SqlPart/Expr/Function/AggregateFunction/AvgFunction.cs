namespace InlineSqlSharp;

public sealed class AvgFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal AvgFunction(Distinct? distinct, AbstractExpr expr)
    {
        _core = new(Keywords.AVG, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
