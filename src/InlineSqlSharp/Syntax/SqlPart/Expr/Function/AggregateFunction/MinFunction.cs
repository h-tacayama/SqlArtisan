namespace InlineSqlSharp;

public sealed class MinFunction(AbstractExpr expr) : AbstractExpr
{
    readonly UnaryFunctionCore _core = new(Keywords.MIN, expr);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
