namespace InlineSqlSharp;

public sealed class LengthBFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;

    internal LengthBFunction(AbstractExpr source)
    {
        _source = source;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.LengthB)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
