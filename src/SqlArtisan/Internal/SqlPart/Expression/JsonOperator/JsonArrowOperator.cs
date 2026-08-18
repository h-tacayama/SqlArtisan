namespace SqlArtisan.Internal;

public sealed class JsonArrowOperator : BinaryOperator
{
    internal JsonArrowOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonArrow, rightSide) { }
}
