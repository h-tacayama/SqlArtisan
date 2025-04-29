namespace InlineSqlSharp;

public sealed class LTrimFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart? _trimChars;

    internal LTrimFunction(
        AbstractExpr source,
        AbstractExpr? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LTrim)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
