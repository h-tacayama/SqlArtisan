namespace InlineSqlSharp;

public sealed class CharacterLeastFunction(CharacterExpr[] expressions) :
    CharacterExpr
{
    private readonly VariadicFunctionCore _core = new(Keywords.LEAST, expressions);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
