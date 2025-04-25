namespace InlineSqlSharp;

public sealed class CountFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal CountFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.COUNT)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
