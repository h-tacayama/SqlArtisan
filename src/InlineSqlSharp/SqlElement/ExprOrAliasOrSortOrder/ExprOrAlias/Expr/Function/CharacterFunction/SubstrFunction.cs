namespace InlineSqlSharp;

public sealed class SubstrFunction(
    CharacterExpr source,
    NumericExpr position,
    NumericExpr? length = null) : CharacterExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.SUBSTR, source, position, length);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
