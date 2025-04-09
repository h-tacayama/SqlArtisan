namespace InlineSqlSharp;

public sealed class DateTimeGreatestFunction(params DateTimeExpr[] expressions)
: DateTimeExpr
{
    private readonly VariadicFunctionCore _core = new(Keywords.GREATEST, expressions);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
