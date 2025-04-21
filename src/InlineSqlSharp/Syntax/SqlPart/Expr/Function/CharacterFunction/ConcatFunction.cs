namespace InlineSqlSharp;

public sealed class ConcatFunction(
    AbstractExpr primary,
    AbstractExpr secondary,
    AbstractExpr[] others) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.CONCAT, [primary, secondary, .. others]);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
