namespace InlineSqlSharp;

public sealed class LowerFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;

    internal LowerFunction(AbstractExpr source)
    {
        _source = source;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Lower)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
