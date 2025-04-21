namespace InlineSqlSharp;

public sealed class LtrimFunction(
    AbstractExpr source,
    AbstractExpr? trimChars = null) : AbstractExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.LTRIM, source, trimChars);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
