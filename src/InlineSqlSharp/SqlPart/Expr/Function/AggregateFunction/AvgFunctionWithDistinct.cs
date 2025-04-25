namespace InlineSqlSharp;

public sealed class AvgFunctionWithDistinct : AbstractExpr
{
    private readonly Distinct _distinct;
    private readonly AbstractSqlPart _expr;

    internal AvgFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.AVG)
        .OpenParenthesis()
        .AppendSpace(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
