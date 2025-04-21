namespace InlineSqlSharp;

public sealed class CountFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal CountFunction(
        AllOrDistinct allOrDistinct,
        AbstractExpr expr)
    {
        _core = new(Keywords.COUNT, allOrDistinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
