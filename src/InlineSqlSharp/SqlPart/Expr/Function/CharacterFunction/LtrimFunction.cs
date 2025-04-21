namespace InlineSqlSharp;

public sealed class LtrimFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal LtrimFunction(
        AbstractExpr source,
        AbstractExpr? trimChars = null)
    {
        _core = new(Keywords.LTRIM, source, trimChars);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
