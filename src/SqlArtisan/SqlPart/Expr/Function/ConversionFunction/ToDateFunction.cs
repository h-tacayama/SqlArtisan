namespace SqlArtisan;

public sealed class ToDateFunction : AbstractExpr
{
    private readonly AbstractSqlPart _text;
    private readonly AbstractSqlPart _format;

    internal ToDateFunction(AbstractExpr text, AbstractExpr format)
    {
        _text = text;
        _format = format;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.ToDate)
        .OpenParenthesis()
        .Append(_text)
        .PrependComma(_format)
        .CloseParenthesis();
}
