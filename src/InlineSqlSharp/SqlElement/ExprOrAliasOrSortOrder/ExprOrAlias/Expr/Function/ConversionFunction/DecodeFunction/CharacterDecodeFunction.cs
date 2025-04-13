namespace InlineSqlSharp;

public sealed class CharacterDecodeFunction(
    object expr,
    (object, object)[] searchResultPairs,
    CharacterExpr @default) : CharacterExpr
{
    private readonly DecodeFunctionCore<CharacterExpr> _core =
        new(expr, searchResultPairs, @default);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
