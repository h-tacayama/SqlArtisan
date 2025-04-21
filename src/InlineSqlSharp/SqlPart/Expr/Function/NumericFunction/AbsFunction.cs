namespace InlineSqlSharp;

public sealed class AbsFunction : AbstractExpr
{
    private readonly UnaryFunctionCore _core;

    internal AbsFunction(AbstractExpr expr)
    {
        _core = new(Keywords.ABS, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
