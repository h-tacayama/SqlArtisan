namespace InlineSqlSharp;

public sealed class AvgFunction(AllOrDistinct allOrDistinct, AbstractExpr expr) :
    AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core =
        new(Keywords.AVG, allOrDistinct, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
