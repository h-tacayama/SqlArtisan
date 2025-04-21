namespace InlineSqlSharp;

public sealed class RtrimFunction(
    AbstractExpr source,
    AbstractExpr? trimChars = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.RTRIM, source, trimChars);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
