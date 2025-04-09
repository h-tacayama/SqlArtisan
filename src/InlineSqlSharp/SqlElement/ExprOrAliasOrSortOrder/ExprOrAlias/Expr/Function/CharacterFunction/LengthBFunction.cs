namespace InlineSqlSharp;

public sealed class LengthBFunction(CharacterExpr source) : NumericExpr
{
    private readonly UnaryFunctionCore _core = new(Keywords.LENGTHB, source);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
