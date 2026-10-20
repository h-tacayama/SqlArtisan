namespace SqlArtisan.Internal;

internal sealed class JsonHashArrowOperator : BinaryOperator
{
    internal JsonHashArrowOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonHashArrow, rightSide) { }
}
