namespace InlineSqlSharp;

public sealed class ToCharFunction : CharacterExpr
{
    private readonly VariadicFunctionCore _core;

    private ToCharFunction(IExpr expr, CharacterExpr? format)
    {
        _core = new(Keywords.TO_CHAR, expr, format);
    }

    public static ToCharFunction Of(
        DateTimeExpr expr,
        CharacterExpr? datetimeFormat = null) => new(expr, datetimeFormat);

    public static ToCharFunction Of(
        NumericExpr expr,
        CharacterExpr? numericFormat = null) => new(expr, numericFormat);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
