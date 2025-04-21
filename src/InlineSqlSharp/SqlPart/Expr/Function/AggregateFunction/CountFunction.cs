namespace InlineSqlSharp;

public sealed class CountFunction(
    AllOrDistinct allOrDistinct,
    AbstractExpr expr) : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core =
        new(Keywords.COUNT, allOrDistinct, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
