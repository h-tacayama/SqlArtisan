namespace InlineSqlSharp;

public sealed class AvgFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal AvgFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.AVG)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
