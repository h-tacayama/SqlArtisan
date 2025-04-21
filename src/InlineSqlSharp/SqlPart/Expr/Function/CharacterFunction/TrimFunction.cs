namespace InlineSqlSharp;

public sealed class TrimFunction(
    AbstractExpr source,
    AbstractExpr? trimChar = null) : AbstractExpr
{
    private readonly AbstractExpr _source = source;
    private readonly AbstractExpr? _trimChar = trimChar;

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.TRIM)
            .OpenParenthesis();

        if (_trimChar is not null)
        {
            buffer.AppendSpace(Keywords.BOTH)
                .AppendSpace(_trimChar)
                .AppendSpace(Keywords.FROM);
        }

        buffer.Append(_source)
            .CloseParenthesis();
    }
}
