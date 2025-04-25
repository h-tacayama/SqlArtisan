namespace InlineSqlSharp;

public sealed class SumFunction : AbstractExpr
{
    private readonly Distinct? _distinct;
    private readonly AbstractSqlPart _expr;

    internal SumFunction(AbstractExpr expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal SumFunction(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.SUM)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
