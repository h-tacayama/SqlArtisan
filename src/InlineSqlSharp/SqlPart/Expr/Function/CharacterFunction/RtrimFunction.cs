namespace InlineSqlSharp;

public sealed class RtrimFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal RtrimFunction(
        AbstractExpr source,
        AbstractExpr? trimChars = null)
    {
        _core = new(Keywords.RTRIM, source, trimChars);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
