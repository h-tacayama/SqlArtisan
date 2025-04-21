namespace InlineSqlSharp;

public sealed class RpadFunction(
    AbstractExpr source,
    AbstractExpr length,
    AbstractExpr? padding = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.RPAD, source, length, padding);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
