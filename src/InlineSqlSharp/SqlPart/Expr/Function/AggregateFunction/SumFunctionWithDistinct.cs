namespace InlineSqlSharp;

public sealed class SumFunctionWithDistinct : AbstractExpr
{
    private readonly Distinct _distinct;
    private readonly AbstractSqlPart _expr;

    internal SumFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.SUM)
        .OpenParenthesis()
        .AppendSpace(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
