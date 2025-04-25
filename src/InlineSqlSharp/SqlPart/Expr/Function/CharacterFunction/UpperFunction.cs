namespace InlineSqlSharp;

public sealed class UpperFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;

    internal UpperFunction(AbstractExpr source)
    {
        _source = source;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.UPPER)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
