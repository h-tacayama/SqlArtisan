namespace InlineSqlSharp;

public sealed class TrimFunction : AbstractExpr
{
    private readonly AbstractExpr _source;
    private readonly AbstractExpr? _trimChar;

    internal TrimFunction(
        AbstractExpr source,
        AbstractExpr? trimChar = null)
    {
        _source = source;
        _trimChar = trimChar;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Trim)
            .OpenParenthesis();

        if (_trimChar is not null)
        {
            buffer.AppendSpace(Keywords.Both)
                .AppendSpace(_trimChar)
                .AppendSpace(Keywords.From);
        }

        buffer.Append(_source)
            .CloseParenthesis();
    }
}
