namespace InlineSqlSharp;

public sealed class AvgFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal AvgFunction(AllOrDistinct allOrDistinct, AbstractExpr expr)
    {
        _core = new(Keywords.AVG, allOrDistinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
