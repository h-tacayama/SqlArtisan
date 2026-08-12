namespace SqlArtisan.Internal;

public sealed class Log10Function : SqlExpression
{
    private readonly SqlExpression _expr;

    internal Log10Function(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Log10)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
