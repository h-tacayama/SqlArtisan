namespace InlineSqlSharp;

public sealed class RegexpSubstrFunction(
    CharacterExpr source,
    CharacterExpr pattern,
    NumericExpr? position = null,
    NumericExpr? occurrence = null,
    RegexpOptions? options = null,
    NumericExpr? subPatternPos = null) : CharacterExpr
{
    private readonly VariadicFunctionCore _core = new(
        Keywords.REGEXP_SUBSTR,
        source,
        pattern,
        position,
        occurrence,
        options?.ToValue(),
        subPatternPos);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
