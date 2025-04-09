namespace InlineSqlSharp;

public sealed class LtrimFunction(
    CharacterExpr source,
    CharacterExpr? trimChars = null) : CharacterExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.LTRIM, source, trimChars);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
