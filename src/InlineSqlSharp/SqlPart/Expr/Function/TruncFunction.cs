namespace InlineSqlSharp;

public sealed class TruncFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr;
    private readonly AbstractSqlPart? _format;

    internal TruncFunction(AbstractExpr expr, AbstractExpr? format = null)
    {
        _expr = expr;
        _format = format;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.TRUNC)
        .OpenParenthesis()
        .Append(_expr)
        .PrependCommaIfNotNull(_format)
        .CloseParenthesis();
}
