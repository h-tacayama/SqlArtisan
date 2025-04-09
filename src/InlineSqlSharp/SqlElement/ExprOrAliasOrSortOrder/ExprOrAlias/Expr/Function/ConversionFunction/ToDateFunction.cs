namespace InlineSqlSharp;

public sealed class ToDateFunction(
    CharacterExpr text,
    CharacterExpr format) : DateTimeExpr
{
    private readonly BinaryFunctionCore _core =
        new(Keywords.TO_DATE, text, format);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
