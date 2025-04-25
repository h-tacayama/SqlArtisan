namespace InlineSqlSharp;

public sealed class CountFunction : AbstractExpr
{
    private readonly Distinct? _distinct;
    private readonly AbstractSqlPart _expr;

    internal CountFunction(AbstractExpr expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal CountFunction(Distinct distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.COUNT)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
