namespace InlineSqlSharp;

public sealed class SumFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal SumFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.SUM)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
