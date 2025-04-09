namespace InlineSqlSharp;

public sealed class DateTimeTruncFunction(
    DateTimeExpr expr,
    CharacterExpr? format = null) : DateTimeExpr
{
    private readonly VariadicFunctionCore _core =
        new(Keywords.TRUNC, expr, format);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
