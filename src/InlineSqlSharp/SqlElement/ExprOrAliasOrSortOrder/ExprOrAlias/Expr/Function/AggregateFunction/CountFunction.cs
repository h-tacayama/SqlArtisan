namespace InlineSqlSharp;

public sealed class CountFunction(AllOrDistinct allOrDistinct, IExpr expr) :
    AggregateFunction
{
    private readonly AllOrDistinctFunctionCore _core =
        new(Keywords.COUNT, allOrDistinct, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
