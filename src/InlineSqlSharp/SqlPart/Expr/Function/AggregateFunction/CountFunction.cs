namespace InlineSqlSharp;

public sealed class CountFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal CountFunction(AbstractExpr expr)
    {
        _core = new(Keywords.COUNT, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
