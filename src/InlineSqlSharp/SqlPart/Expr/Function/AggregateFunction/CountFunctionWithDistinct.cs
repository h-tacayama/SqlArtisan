namespace InlineSqlSharp;

public sealed class CountFunctionWithDistinct : AbstractExpr
{
    private readonly UnaryFunctionWithDistinct _core;

    internal CountFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _core = new(Keywords.COUNT, distinct, expr);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
