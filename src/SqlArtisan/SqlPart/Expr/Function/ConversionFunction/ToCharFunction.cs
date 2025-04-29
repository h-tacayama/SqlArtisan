namespace InlineSqlSharp;

public sealed class ToCharFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;
    private readonly AbstractSqlPart? _format;

    internal ToCharFunction(
        AbstractExpr expr,
        AbstractExpr? format = null)
    {
        _expr = expr;
        _format = format;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ToChar)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_format)
        .CloseParenthesis();
}
