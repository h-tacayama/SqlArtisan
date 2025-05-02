namespace SqlArtisan;

public sealed class ToCharFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression? _format;

    internal ToCharFunction(
        SqlExpression expr,
        SqlExpression? format = null)
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
