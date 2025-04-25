namespace InlineSqlSharp;

public sealed class ToNumberFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;
    private readonly AbstractSqlPart? _format;

    internal ToNumberFunction(
        AbstractExpr expr,
        AbstractExpr? format = null)
    {
        _expr = expr;
        _format = format;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.TO_NUMBER)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_format)
        .CloseParenthesis();
}
