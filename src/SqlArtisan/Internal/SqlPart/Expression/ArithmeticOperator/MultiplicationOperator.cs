namespace SqlArtisan.Internal;

internal sealed class MultiplicationOperator : BinaryOperator
{
    internal MultiplicationOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Asterisk, rightSide) { }
}
