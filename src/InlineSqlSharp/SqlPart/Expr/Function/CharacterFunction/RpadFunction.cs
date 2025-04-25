namespace InlineSqlSharp;

public sealed class RpadFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart _length;
    private readonly AbstractSqlPart? _padding;

    internal RpadFunction(
        AbstractExpr source,
        AbstractExpr length,
        AbstractExpr? padding = null)
    {
        _source = source;
        _length = length;
        _padding = padding;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RPAD)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_length)
        .PrependCommaIfNotNull(_padding)
        .CloseParenthesis();
}
