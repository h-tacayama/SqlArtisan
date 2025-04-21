namespace InlineSqlSharp;

public sealed class ReplaceFunction : AbstractExpr
{
    private readonly TernaryFunctionCore _core;

    internal ReplaceFunction(
        AbstractExpr source,
        AbstractExpr search,
        AbstractExpr replacement)
    {
        _core = new(Keywords.REPLACE, source, search, replacement);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
