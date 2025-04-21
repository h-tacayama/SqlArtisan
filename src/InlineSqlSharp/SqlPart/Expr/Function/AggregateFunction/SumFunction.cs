namespace InlineSqlSharp;

public sealed class SumFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal SumFunction(
        AllOrDistinct allOrDistinct,
        AbstractExpr expr)
    {
        _core = new(Keywords.SUM, allOrDistinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
