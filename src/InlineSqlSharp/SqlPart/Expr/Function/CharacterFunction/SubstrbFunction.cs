namespace InlineSqlSharp;

public sealed class SubstrbFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart _position;
    private readonly AbstractSqlPart? _length;

    internal SubstrbFunction(
        AbstractExpr source,
        AbstractExpr position,
        AbstractExpr? length = null)
    {
        _source = source;
        _position = position;
        _length = length;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.SUBSTRB)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_position)
        .PrependCommaIfNotNull(_length)
        .CloseParenthesis();
}
