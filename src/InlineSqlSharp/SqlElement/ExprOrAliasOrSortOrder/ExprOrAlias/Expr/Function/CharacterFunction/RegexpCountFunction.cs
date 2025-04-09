namespace InlineSqlSharp;

public sealed class RegexpCountFunction(
    CharacterExpr source,
    CharacterExpr pattern,
    NumericExpr? position = null,
    RegexpOptions? options = null) : NumericExpr
{
    private readonly VariadicFunctionCore _core = new(
        Keywords.REGEXP_COUNT,
        source,
        pattern,
        position,
        options?.ToValue());

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
