namespace InlineSqlSharp;

public sealed class MaxFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;

    internal MaxFunction(AbstractExpr expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Max)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
