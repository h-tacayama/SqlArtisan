namespace SqlArtisan.Internal;

public sealed class QuantifiedExpression(string keyword, SqlExpression operand) : SqlExpression
{
    private readonly SqlExpression _operand = operand;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(keyword)
        .AppendSpace()
        .EncloseInParentheses(_operand);
}
