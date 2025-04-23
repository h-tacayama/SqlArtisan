namespace InlineSqlSharp;

public sealed class CountFunction : AbstractExpr
{
    private readonly AllOrDistinctFunctionCore _core;

    internal CountFunction(Distinct? distinct, AbstractExpr expr)
    {
        _core = new(Keywords.COUNT, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
