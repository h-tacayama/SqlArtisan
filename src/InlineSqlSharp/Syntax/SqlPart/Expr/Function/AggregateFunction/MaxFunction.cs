namespace InlineSqlSharp;

public sealed class MaxFunction(AbstractExpr expr) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.MAX, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
