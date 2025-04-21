namespace InlineSqlSharp;

public sealed class SumFunction(
    AllOrDistinct allOrDistinct,
    AbstractExpr expr) : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core =
        new(Keywords.SUM, allOrDistinct, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
