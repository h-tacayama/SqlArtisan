namespace SqlArtisan.Internal;

public sealed class QuantifiedExpression : SqlExpression
{
    private readonly string _keyword;
    private readonly SqlExpression _operand;

    internal QuantifiedExpression(string keyword, SqlExpression operand)
    {
        _keyword = keyword;
        _operand = operand;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_keyword)
        .AppendSpace()
        .EncloseInParentheses(_operand);
}
