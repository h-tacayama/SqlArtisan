namespace InlineSqlSharp;

public sealed class NumericMinFunction(NumericExpr expr) : NumericExpr
{
    readonly UnaryFunctionCore _core = new(Keywords.MIN, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
