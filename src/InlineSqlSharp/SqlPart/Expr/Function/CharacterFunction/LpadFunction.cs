namespace InlineSqlSharp;

public sealed class LpadFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal LpadFunction(
        AbstractExpr source,
        AbstractExpr length,
        AbstractExpr? padding = null)
    {
        _core = new(Keywords.LPAD, source, length, padding);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
