namespace InlineSqlSharp;

public sealed class LtrimFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart? _trimChars;

    internal LtrimFunction(
        AbstractExpr source,
        AbstractExpr? trimChars = null)
    {
        _source = source;
        _trimChars = trimChars;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LTRIM)
        .OpenParenthesis()
        .Append(_source)
        .PrependCommaIfNotNull(_trimChars)
        .CloseParenthesis();
}
