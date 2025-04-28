namespace InlineSqlSharp;

public sealed class LengthFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;

    internal LengthFunction(AbstractExpr source)
    {
        _source = source;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Length)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
