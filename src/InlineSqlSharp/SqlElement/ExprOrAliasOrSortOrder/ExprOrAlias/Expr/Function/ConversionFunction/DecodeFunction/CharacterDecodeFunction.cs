namespace InlineSqlSharp;

public sealed class CharacterDecodeFunction : CharacterExpr
{
    private readonly DecodeFunctionCore _core;

    public CharacterDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        CharacterExpr @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public CharacterDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        char @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public CharacterDecodeFunction(
        object expr,
        (object, object)[] searchResultPairs,
        string @default)
    {
        _core = new DecodeFunctionCore(expr, searchResultPairs, @default);
    }

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
