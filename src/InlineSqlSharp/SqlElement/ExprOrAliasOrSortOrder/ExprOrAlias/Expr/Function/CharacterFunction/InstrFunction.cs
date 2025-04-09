namespace InlineSqlSharp;

public sealed class InstrFunction(
    CharacterExpr source,
    CharacterExpr substring,
    NumericExpr? position = null,
    NumericExpr? occurrence = null) : NumericExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.INSTR, source, substring, position, occurrence);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
