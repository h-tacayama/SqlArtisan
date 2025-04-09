namespace InlineSqlSharp;

public sealed class NumericGreatestFunction(NumericExpr[] expressions) :
    NumericExpr
{
    private readonly VariadicFunctionCore _core = new(Keywords.GREATEST, expressions);

    public override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
