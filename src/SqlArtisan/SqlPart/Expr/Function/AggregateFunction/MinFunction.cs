namespace InlineSqlSharp;

public sealed class MinFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal MinFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Min)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
