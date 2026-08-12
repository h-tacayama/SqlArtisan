namespace SqlArtisan.Internal;

public sealed class IsnullFunction : SqlExpression
{
    private readonly SqlExpression _expr1;
    private readonly SqlExpression _expr2;

    internal IsnullFunction(SqlExpression expr1, SqlExpression expr2)
    {
        _expr1 = expr1;
        _expr2 = expr2;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Isnull)
        .OpenParenthesis()
        .Append(_expr1)
        .PrependComma(_expr2)
        .CloseParenthesis();
}
