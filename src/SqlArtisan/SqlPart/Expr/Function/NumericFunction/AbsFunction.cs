namespace InlineSqlSharp;

public sealed class AbsFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal AbsFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Abs)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
