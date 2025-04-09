namespace InlineSqlSharp;

public sealed class AvgFunction(AllOrDistinct allOrDistinct, IExpr expr) :
    AggregateFunction
{
    private readonly AllOrDistinctFunctionCore _core =
        new(Keywords.AVG, allOrDistinct, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
