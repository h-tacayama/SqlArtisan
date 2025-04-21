namespace InlineSqlSharp;

public sealed class AbsFunction(AbstractExpr expr) : AbstractExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.ABS, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
