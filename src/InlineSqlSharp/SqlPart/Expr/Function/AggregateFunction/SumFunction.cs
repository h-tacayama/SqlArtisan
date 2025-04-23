namespace InlineSqlSharp;

public sealed class SumFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal SumFunction(Distinct? distinct, AbstractExpr expr)
    {
        _core = new(Keywords.SUM, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
