namespace InlineSqlSharp;

public sealed class DateTimeMinFunction(DateTimeExpr expr) : DateTimeExpr
{
    readonly UnaryFunctionCore _core = new(Keywords.MIN, expr);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
