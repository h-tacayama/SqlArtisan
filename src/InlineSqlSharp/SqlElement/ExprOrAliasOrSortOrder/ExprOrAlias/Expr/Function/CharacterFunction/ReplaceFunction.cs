namespace InlineSqlSharp;

public sealed class ReplaceFunction(
    CharacterExpr source,
    CharacterExpr search,
    CharacterExpr replacement) : CharacterExpr
{
    private readonly TernaryFunctionCore _core =
        new(Keywords.REPLACE, source, search, replacement);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}