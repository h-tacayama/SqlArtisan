namespace InlineSqlSharp;

public sealed class AvgFunction : AbstractExpr
{
    private readonly Distinct? _distinct;
    private readonly AbstractSqlPart _expr;

    internal AvgFunction(AbstractExpr expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal AvgFunction(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.AVG)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
