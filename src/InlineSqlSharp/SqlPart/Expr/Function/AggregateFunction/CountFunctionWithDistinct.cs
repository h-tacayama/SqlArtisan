namespace InlineSqlSharp;

public sealed class CountFunctionWithDistinct : AbstractExpr
{
    private readonly Distinct _distinct;
    private readonly AbstractSqlPart _expr;

    internal CountFunctionWithDistinct(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.COUNT)
        .OpenParenthesis()
        .AppendSpace(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
