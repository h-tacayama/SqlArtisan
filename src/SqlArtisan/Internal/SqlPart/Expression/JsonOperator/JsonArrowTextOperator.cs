namespace SqlArtisan.Internal;

internal sealed class JsonArrowTextOperator : BinaryOperator
{
    internal JsonArrowTextOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonArrowText, rightSide) { }
}
